module CommandHandler

open System.Collections.Generic
open FilterParser
open FilterApplier
open Domain
open System.Collections.Concurrent

let commandHandler (tables: Tables) command =
  match command with
  | CreateTable name ->
      match tables.TryAdd(name, ConcurrentDictionary()) with
      | true -> Ack
      | false -> Conflict TableAlreadyExists
  | InsertOrMerge (table, (keys, fields)) ->
      let table = tables.[table]
      match table.ContainsKey keys with
      | true ->
          table.[keys] <- fields
          Ack
      | false ->
          table.Add(keys, fields) |> ignore
          Ack
  | Insert (table, (keys, fields)) ->
      let table = tables.[table]
      match table.ContainsKey keys with
      | true -> Conflict KeyAlreadyExists
      | false ->
          table.Add(keys, fields) |> ignore
          Ack
  | Get (table, keys) ->
      let table = tables.[table]
      match table.TryGetValue(keys) with
      | true, fields -> GetResponse(keys, fields)
      | _ -> NotFound
  | Query (table, filter) ->
      let table = tables.[table]

      let matchingRows =
        match parse filter with
        | Result.Ok result -> applyFilter table result
        | Result.Error error ->
            printfn "Filter: %A;\nError: %A" filter error
            Seq.empty

      matchingRows |> Seq.toList |> QueryResponse
  | Delete (table, keys) ->
      let table = tables.[table]
      table.Remove(keys) |> ignore
      Ack
