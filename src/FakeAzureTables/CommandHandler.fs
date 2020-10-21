module CommandHandler

open FilterParser
open FilterApplier
open Domain
open LiteDB
open Bson
open Database

let tableCommandHandler (db: ILiteDatabase) command =
  match command with
  | CreateTable table ->
      match db.TableExists table, tableNameIsValid table with
      | true, _ -> TableCommandResponse.Conflict TableAlreadyExists
      | _, false -> TableCommandResponse.Conflict InvalidTableName
      | _ ->
          db.GetTable table |> ignore
          TableCommandResponse.Ack

  | ListTables ->
      db.TablesCollection.FindAll()
      |> Seq.map (fun kvp -> kvp.Key)
      |> TableCommandResponse.TableList

let (|ETagMatches|_|) (existingETag: ETag) (existingRow: TableRow option) =
  match existingRow, existingETag with
  | Some existingRow, ETag.Specific existingETag when (existingRow.ETag |> ETag.serialize) =
                                                        (existingETag |> ETag.serialize) -> Some()
  | Some _, ETag.All -> Some()
  | _ -> None

let writeCommandHandler (db: ILiteDatabase) command =
  match command with
  | InsertOrMerge (table, row) ->
      let table = db.GetTable table
      let etag = ETag.create ()

      let row =
        match row.Keys
              |> TableKeys.toBsonExpression
              |> table.TryFindOne with
        | Some existingRow -> row |> TableRow.merge existingRow
        | _ -> row

      row
      |> TableRow.withETag etag
      |> table.Upsert
      |> ignore
      WriteCommandResponse.Ack(row.Keys, etag)
  | InsertOrReplace (table, row) ->
      let table = db.GetTable table
      let etag = ETag.create ()
      row
      |> TableRow.withETag etag
      |> table.Upsert
      |> ignore
      WriteCommandResponse.Ack(row.Keys, etag)
  | Insert (table, row) ->
      let table = db.GetTable table
      let etag = ETag.create ()
      match row |> TableRow.withETag etag |> table.TryInsert with
      | true -> WriteCommandResponse.Ack(row.Keys, etag)
      | false -> WriteCommandResponse.Conflict KeyAlreadyExists
  | Replace (table, existingETag, row) ->
      let table = db.GetTable table
      let etag = ETag.create ()
      match row.Keys
            |> TableKeys.toBsonExpression
            |> table.TryFindOne,
            existingETag with
      | Some existingRow, Specific existingETag when (existingRow.ETag |> ETag.serialize) =
                                                       (existingETag |> ETag.serialize) ->
          match table.Update(row |> TableRow.withETag etag) with
          | true -> WriteCommandResponse.Ack(row.Keys, etag)
          | _ -> WriteCommandResponse.Conflict EntityDoesntExist
      | Some _, Specific _ -> WriteCommandResponse.Conflict UpdateConditionNotSatisfied
      | _ -> WriteCommandResponse.Conflict EntityDoesntExist
  | Merge (table, existingETag, row) ->
      let table = db.GetTable table
      let etag = ETag.create ()
      match row.Keys
            |> TableKeys.toBsonExpression
            |> table.TryFindOne,
            existingETag with
      | Some existingRow, Specific existingETag when (existingRow.ETag |> ETag.serialize) =
                                                       (existingETag |> ETag.serialize) ->
          match table.Update
                  (row
                   |> TableRow.merge existingRow
                   |> TableRow.withETag etag) with
          | true -> WriteCommandResponse.Ack(row.Keys, etag)
          | _ -> WriteCommandResponse.Conflict EntityDoesntExist
      | Some _, Specific _ -> WriteCommandResponse.Conflict UpdateConditionNotSatisfied
      | _ -> WriteCommandResponse.Conflict EntityDoesntExist
  | Delete (table, existingETag, keys) ->
      let table = db.GetTable table
      match keys
            |> TableKeys.toBsonExpression
            |> table.TryFindOne with
      | ETagMatches existingETag ->
          table.DeleteMany(keys |> TableKeys.toBsonExpression)
          |> ignore
          WriteCommandResponse.Ack(keys, Missing)
      | _ -> WriteCommandResponse.Conflict UpdateConditionNotSatisfied

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
