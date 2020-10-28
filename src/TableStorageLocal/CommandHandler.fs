namespace TableStorageLocal

module CommandHandler =

  open Domain
  open LiteDB
  open Bson
  open Database
  open System.Collections.Generic

  let private tableCommandHandler (db: ILiteDatabase) command =
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

  let private writeCommandHandler (db: ILiteDatabase) command =
    match command with
    | InsertOrMerge (table, row) ->
        let table = db.GetTable table
        let etag = ETag.create ()

        let row =
          match row.Id |> table.TryFindById with
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
        | false -> WriteCommandResponse.Conflict EntityAlreadyExists
    | Replace (table, existingETag, row) ->
        let table = db.GetTable table
        let etag = ETag.create ()
        match row.Id |> table.TryFindById with
        | TableRow.ExistsWithMatchingETag existingETag existingRow ->
            match table.Update(row |> TableRow.withETag etag) with
            | true -> WriteCommandResponse.Ack(row.Keys, etag)
            | _ -> WriteCommandResponse.NotFound ResourceNotFound
        | TableRow.ExistsWithDifferentETag existingETag _ ->
            WriteCommandResponse.PreconditionFailed UpdateConditionNotSatisfied
        | _ -> WriteCommandResponse.NotFound ResourceNotFound
    | Merge (table, existingETag, row) ->
        let table = db.GetTable table
        let etag = ETag.create ()
        match row.Id |> table.TryFindById with
        | TableRow.ExistsWithMatchingETag existingETag existingRow ->
            match table.Update
                    (row
                     |> TableRow.merge existingRow
                     |> TableRow.withETag etag) with
            | true -> WriteCommandResponse.Ack(row.Keys, etag)
            | _ -> WriteCommandResponse.NotFound ResourceNotFound
        | TableRow.ExistsWithDifferentETag existingETag _ ->
            WriteCommandResponse.PreconditionFailed UpdateConditionNotSatisfied
        | _ -> WriteCommandResponse.NotFound ResourceNotFound
    | Delete (table, existingETag, keys) ->
        let table = db.GetTable table
        match keys.Id |> table.TryFindById with
        | TableRow.ExistsWithMatchingETag existingETag _ ->
            table.DeleteMany(keys |> TableKeys.toBsonExpression)
            |> ignore
            WriteCommandResponse.Ack(keys, Missing)
        | TableRow.ExistsWithDifferentETag existingETag _ ->
            WriteCommandResponse.PreconditionFailed UpdateConditionNotSatisfied
        | _ -> WriteCommandResponse.NotFound ResourceNotFound

  let private applySelect fields (tableRows: TableRow array) =
    match fields with
    | Select.All -> tableRows
    | Select.Fields fields ->
        tableRows
        |> Array.map (fun f ->

             let filteredFields =
               f.Fields
               |> Seq.filter (fun kvp -> fields |> List.contains kvp.Key)
               |> Dictionary

             { f with Fields = filteredFields })

  let private readCommandHandler (db: ILiteDatabase) command =
    match command with
    | Get (table, keys) ->
        let table = db.GetTable table
        match keys.Id |> table.TryFindById with
        | Some row -> GetResponse(row)
        | _ -> ReadCommandResponse.NotFoundResponse
    | Query query ->
        let table = db.GetTable query.Table

        let rows, continuation =
          QueryApplier.query table query.Filter query.Top query.Continuation

        let rows = rows |> applySelect query.Select

        QueryResponse(rows, continuation)

  let batchCommandHandler (db: ILiteDatabase) writeCommandHandler command =

    let containsDuplicateRowKeys =
      let rowKeys =
        command.Commands
        |> List.distinctBy (fun c -> c.TableKeys.RowKey)

      rowKeys.Length < command.Commands.Length

    if containsDuplicateRowKeys then
      BadRequest InvalidDuplicateRow
    else
      try
        db.BeginTrans() |> ignore

        let commandResults =
          command.Commands |> List.map writeCommandHandler

        let commandResults =
          match commandResults
                |> List.forall WriteCommandResponse.isSuccess with
          | true ->
              db.Commit() |> ignore
              commandResults
          | false ->
              db.Rollback() |> ignore
              commandResults
              |> List.filter (WriteCommandResponse.isSuccess >> not)

        commandResults |> WriteResponses
      with ex ->
        db.Rollback() |> ignore
        reraise ()

  let commandHandler (db: ILiteDatabase) command =
    let tableCommandHandler = tableCommandHandler db
    let writeCommandHandler = writeCommandHandler db
    let readCommandHandler = readCommandHandler db

    let batchCommandHandler =
      batchCommandHandler db writeCommandHandler

    match command with
    | Table command -> command |> tableCommandHandler |> TableResponse
    | Write command -> command |> writeCommandHandler |> WriteResponse
    | Read command -> command |> readCommandHandler |> ReadResponse
    | Batch command -> command |> batchCommandHandler |> BatchResponse
