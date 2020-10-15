module CommandHandler

open FilterParser
open FilterApplier
open Domain
open LiteDB
open Bson
open System.Text.RegularExpressions
open System
open System.Collections.Generic

let randomString =
  let chars =
    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"

  let random = Random()

  let generate length =
    Array.init length (fun _ ->
      (random.Next(0, chars.Length - 1), chars)
      ||> Seq.item)
    |> String

  generate

type ILiteDatabase with

  member __.TablesCollection =
    let col =
      __.GetCollection<KeyValuePair<string, string>> "_tables"

    col.EnsureIndex(fun kvp -> kvp.Key) |> ignore
    col

  member __.GetCollectionName table =
    __.TablesCollection.Find(fun kvp -> kvp.Key = table)
    |> Seq.tryHead
    |> Option.map (fun kvp -> kvp.Value)

  member __.TableExists table =
    __.GetCollectionName table |> Option.isSome

  member __.GetTable table =
    let collectionName =
      match __.GetCollectionName table with
      | Some collectionName -> collectionName
      | _ ->
          let kvp = KeyValuePair(table, randomString 12)
          __.TablesCollection.Insert kvp |> ignore
          kvp.Value

    let col =
      __.GetCollection<TableRow> collectionName

    col.EnsureIndex(fun tableRow -> tableRow.Id)
    |> ignore
    col

type ILiteCollection<'T> with
  member __.TryInsert(row: 'T) =
    try
      __.Insert row |> ignore
      true
    with
    | :? LiteException as ex when ex.ErrorCode = LiteException.INDEX_DUPLICATE_KEY -> false
    | _ -> reraise ()

  member __.TryFindOne(predicate: BsonExpression) = __.Find predicate |> Seq.tryHead

let tableNameIsValid tableName =
  Regex.IsMatch(tableName, "^[A-Za-z0-9]{2,62}$")

let tableCommandHandler (db: ILiteDatabase) command =
  match command with
  | Table command ->
      match command with
      | CreateTable table ->
          match db.TableExists table, tableNameIsValid table with
          | true, _ -> Conflict TableAlreadyExists
          | _, false -> Conflict InvalidTableName
          | _ ->
              db.GetTable table |> ignore
              Ack
  | Write command ->
      match command with
      | InsertOrMerge (table, newRow) ->
          let table = db.GetTable table

          let row =
            match newRow.Keys
                  |> TableKeys.toBsonExpression
                  |> table.TryFindOne with
            | Some existingRow ->
                let mergedRow = { newRow with Fields = Dictionary() }
                for (KeyValue (name, existingValue)) in existingRow.Fields do
                  match newRow.Fields.TryGetValue name with
                  | true, newValue -> mergedRow.Fields.Add(name, newValue)
                  | _ -> mergedRow.Fields.Add(name, existingValue)
                mergedRow
            | _ -> newRow

          table.Upsert row |> ignore
          Ack
      | InsertOrReplace (table, row) ->
          let table = db.GetTable table
          table.Upsert row |> ignore
          Ack
      | Insert (table, row) ->
          let table = db.GetTable table
          match table.TryInsert row with
          | true -> Ack
          | false -> Conflict KeyAlreadyExists
      | Delete (table, keys) ->
          let table = db.GetTable table
          table.DeleteMany(keys |> TableKeys.toBsonExpression)
          |> ignore
          Ack
  | Read command ->
      match command with
      | Get (table, keys) ->
          let table = db.GetTable table
          match keys
                |> TableKeys.toBsonExpression
                |> table.TryFindOne with
          | Some row -> GetResponse row
          | _ -> NotFound
      | Query (table, filter) ->
          let table = db.GetTable table

let writeCommandHandler (db: ILiteDatabase) command =
  match command with
  | InsertOrMerge (table, row) ->
      let table = db.GetTable table
      table.TryInsert row |> ignore
      Ack(row.Keys, System.DateTimeOffset.UtcNow)
  | InsertOrReplace (table, row) ->
      let table = db.GetTable table
      table.TryInsert row |> ignore
      Ack(row.Keys, System.DateTimeOffset.UtcNow)
  | Insert (table, row) ->
      let table = db.GetTable table
      match table.TryInsert row with
      | true -> Ack(row.Keys, System.DateTimeOffset.UtcNow)
      | false -> Conflict KeyAlreadyExists
  | Delete (table, keys) ->
      let table = db.GetTable table
      table.DeleteMany(keys |> TableKeys.toBsonExpression)
      |> ignore
      Ack(keys, System.DateTimeOffset.UtcNow)

let readCommandHandler (db: ILiteDatabase) command =
  match command with
  | Get (table, keys) ->
      let table = db.GetTable table
      match keys
            |> TableKeys.toBsonExpression
            |> table.TryFindOne with
      | Some row -> GetResponse(row)
      | _ -> ReadCommandResponse.NotFoundResponse
  | Query (table, filter) ->
      let table = db.GetTable table

      let matchingRows =
        match parse filter with
        | Ok result -> applyFilter table result
        | Error error ->
            printfn "Filter: %A;\nError: %A" filter error
            Seq.empty

      matchingRows |> Seq.toList |> QueryResponse

let commandHandler (db: ILiteDatabase) command =
  let tableCommandHandler = tableCommandHandler db
  let writeCommandHandler = writeCommandHandler db
  let readCommandHandler = readCommandHandler db
  match command with
  | Table command -> command |> tableCommandHandler |> TableResponse
  | Write command -> command |> writeCommandHandler |> WriteResponse
  | Read command -> command |> readCommandHandler |> ReadResponse
  | Batch batch ->
      match db.BeginTrans() with
      | true ->
          try
            let commandResults =
              batch.Commands |> List.map writeCommandHandler

            match db.Commit() with
            | true ->
                { CommandResponses = commandResults }
                |> BatchResponse
            | false -> failwithf "Failed to commit a transaction"
          with ex ->
            db.Rollback() |> ignore
            reraise ()
      | false -> failwithf "Failed to create a transaction"
