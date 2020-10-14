module CommandHandler

open FilterParser
open FilterApplier
open Domain
open LiteDB
open Bson

type ILiteDatabase with
  member __.TableExists = __.CollectionExists

  member __.GetTable(table: string) =
    let col = __.GetCollection<TableRow> table
    col.EnsureIndex("PK_RK_UNIQUE", BsonExpression.Create("LOWER($.Keys.PartitionKey + $.Keys.RowKey)"), true)
    |> ignore
    col

type ILiteCollection<'T> with
  member __.TryInsert(row: 'T) =
    try
      __.Insert(row) |> ignore
      true
    with ex -> if ex.Message.Contains("PK_RK_UNIQUE") then false else reraise ()

  member __.TryFindOne(predicate: BsonExpression) = __.Find(predicate) |> Seq.tryHead

let commandHandler (db: ILiteDatabase) command =
  match command with
  | Table command ->
      match command with
      | CreateTable table ->
          match db.TableExists table with
          | true -> Conflict TableAlreadyExists
          | false ->
              db.GetTable table |> ignore
              Ack
  | Write command ->
      match command with
      | InsertOrMerge (table, row) ->
          let table = db.GetTable table
          table.TryInsert row |> ignore
          Ack
      | InsertOrReplace (table, row) ->
          let table = db.GetTable table
          table.TryInsert row |> ignore
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
          | Some row -> GetResponse(row)
          | _ -> NotFound
      | Query (table, filter) ->
          let table = db.GetTable table

          let matchingRows =
            match parse filter with
            | Result.Ok result -> applyFilter table result
            | Result.Error error ->
                printfn "Filter: %A;\nError: %A" filter error
                Seq.empty

          matchingRows |> Seq.toList |> QueryResponse
  | Batch command -> NotFound
