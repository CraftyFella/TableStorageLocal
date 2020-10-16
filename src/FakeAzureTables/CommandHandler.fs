module CommandHandler

open FilterParser
open FilterApplier
open Domain
open LiteDB
open Bson
open System
open System.Collections.Generic
open System.Text.RegularExpressions

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

let withETag (etag: DateTimeOffset) (row: TableRow) =
  row.Fields.TryAdd("Timestamp", FieldValue.Date etag)
  |> ignore
  row

let tableCommandHandler (db: ILiteDatabase) command =
  match command with
  | CreateTable table ->
      match db.TableExists table, tableNameIsValid table with
      | true, _ -> TableCommandResponse.Conflict TableAlreadyExists
      | _, false -> TableCommandResponse.Conflict InvalidTableName
      | _ ->
          db.GetTable table |> ignore
          TableCommandResponse.Ack

let writeCommandHandler (db: ILiteDatabase) command =
  match command with
  | InsertOrMerge (table, row) ->
      let table = db.GetTable table

      let row =
        match row.Keys
              |> TableKeys.toBsonExpression
              |> table.TryFindOne with
        | Some existingRow ->
            for (KeyValue (name, field)) in row.Fields do
              match existingRow.Fields.ContainsKey name with
              | true -> existingRow.Fields.[name] <- field
              | _ -> existingRow.Fields.Add(name, field)
            existingRow
        | _ -> row

      table.Upsert row |> ignore
      WriteCommandResponse.Ack(row.Keys, System.DateTimeOffset.UtcNow)
  | InsertOrReplace (table, row) ->
      let table = db.GetTable table
      table.Upsert row |> ignore
      WriteCommandResponse.Ack(row.Keys, System.DateTimeOffset.UtcNow)
  | Insert (table, row) ->
      let table = db.GetTable table
      let etag = System.DateTimeOffset.UtcNow
      match row |> withETag etag |> table.TryInsert with
      | true -> WriteCommandResponse.Ack(row.Keys, etag)
      | false -> WriteCommandResponse.Conflict KeyAlreadyExists
  | Replace (table, existingETag, row) ->
      let table = db.GetTable table
      let etag = System.DateTimeOffset.UtcNow
      match row.Keys
            |> TableKeys.toBsonExpression
            |> table.TryFindOne with
      | Some existing when (existing.ETag |> ETag.fromDateTimeOffset) = (existingETag |> ETag.fromDateTimeOffset) ->
          match table.Update(row |> withETag etag) with
          | true -> WriteCommandResponse.Ack(row.Keys, etag)
          | _ -> WriteCommandResponse.Conflict EntityDoesntExist
      | Some _ -> WriteCommandResponse.Conflict UpdateConditionNotSatisfied
      | _ -> WriteCommandResponse.Conflict EntityDoesntExist
  | Delete (table, keys) ->
      let table = db.GetTable table
      table.DeleteMany(keys |> TableKeys.toBsonExpression)
      |> ignore
      WriteCommandResponse.Ack(keys, System.DateTimeOffset.UtcNow)

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
        match filter with
        | None -> applyFilter table Filter.All
        | Some filter ->
            match parse filter with
            | Ok filter -> applyFilter table filter
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
      db.BeginTrans() |> ignore
      try

        let commandResults =
          batch.Commands |> List.map writeCommandHandler

        db.Commit() |> ignore

        { CommandResponses = commandResults }
        |> BatchResponse
      with ex ->
        db.Rollback() |> ignore
        reraise ()
