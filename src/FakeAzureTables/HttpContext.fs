module HttpContext

open Newtonsoft.Json.Linq
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks
open System.Threading.Tasks
open System.Text.RegularExpressions
open Domain
open Http

module Result =
  let isOk result =
    match result with
    | Ok _ -> true
    | _ -> false

  let valueOf result =
    match result with
    | Ok v -> v
    | _ -> failwithf "shouldn't get here"

module Option =

  let valueOf result =
    match result with
    | Some v -> v
    | _ -> failwithf "shouldn't get here"

module WriteCommand =

  let isWriteCommand command =
    match command with
    | Write c -> true
    | _ -> false

  let valueOf command =
    match command with
    | Write c -> c
    | _ -> failwithf "shouldn't get here"

module private Request =

  let (|Regex|_|) pattern input =
    match Regex.Match(input, pattern) with
    | m when m.Success ->
        m.Groups
        |> Seq.skip 1
        |> Seq.map (fun g -> g.Value)
        |> Seq.toList
        |> Some
    | _ -> None

  let private (|QueryRequest|_|) (request: Request) =
    match request.Method, request.Path with
    | Method.Get, Regex "^\/devstoreaccount1\/(\w+)$" [ tableName ] ->
        match request.Query.ContainsKey("$filter") with
        | true -> Some(tableName, request.Query.Item "$filter" |> Array.tryHead)
        | false -> Some(tableName, None)
    | _ -> None

  let private (|CreateTableRequest|_|) (request: Request) =
    match request.Method, request.Path with
    | Method.Post, "/devstoreaccount1/Tables"
    | Method.Post, "/devstoreaccount1/Tables()" ->
        let jObject = JObject.Parse request.Body
        match jObject.TryGetValue "TableName" with
        | true, tableName -> Some(string tableName)
        | _ -> None
    | _ -> None

  let private (|ListTablesRequest|_|) (request: Request) =
    match request.Method, request.Path with
    | Method.Get, "/devstoreaccount1/Tables" -> Some()
    | _ -> None

  let private (|InsertRequest|_|) (request: Request) =
    match request.Method, request.Path with
    | Method.Post, Regex "^\/devstoreaccount1\/(\w+)\(\)$" [ tableName; ]
    | Method.Post, Regex "^\/devstoreaccount1\/(\w+)$" [ tableName; ] ->
        let jObject = JObject.Parse request.Body
        match jObject.TryGetValue "PartitionKey" with
        | true, p ->
            match jObject.TryGetValue "RowKey" with
            | true, r -> Some(tableName, string p, string r, jObject)
            | _ -> None
        | _ -> None
    | _ -> None

  let private (|ReplaceRequest|MergeRequest|InsertOrMergeRequest|InsertOrReplaceRequest|DeleteRequest|GetRequest|NotFoundRequest|) (request: Request) =
    match request.Path with
    | Regex "^\/devstoreaccount1\/(\w+)\(PartitionKey='(.+)',RowKey='(.+)'\)$" [ tableName; partitionKey; rowKey ] ->
        match request.Method with
        | Method.Post ->
            let jObject = JObject.Parse request.Body
            match request.Headers.TryGetValue("If-Match") with
            | true, [| etag |] -> MergeRequest(tableName, partitionKey, rowKey, etag |> ETag.parse, jObject)
            | _ -> InsertOrMergeRequest(tableName, partitionKey, rowKey, jObject)
        | Method.Put ->
            let jObject = JObject.Parse request.Body
            match request.Headers.TryGetValue("If-Match") with
            | true, [| etag |] -> ReplaceRequest(tableName, partitionKey, rowKey, etag |> ETag.parse, jObject)
            | _ -> InsertOrReplaceRequest(tableName, partitionKey, rowKey, jObject)
        | Method.Get -> GetRequest(tableName, partitionKey, rowKey)
        | Method.Delete -> DeleteRequest(tableName, partitionKey, rowKey)
        | _ -> NotFoundRequest
    | _ -> NotFoundRequest

  let private (|BatchRequest|_|) (request: Request) =
    match request.Method, request.Path with
    | Method.Post, "/devstoreaccount1/$batch" ->
        match (request.Headers.Item "Content-Type")
              |> Array.head with
        | Regex "boundary=(.+)$" [ boundary ] ->
            let rawRequests =
              Regex.Split(request.Body, "--changeset_.+$", RegexOptions.Multiline ||| RegexOptions.IgnoreCase)
              |> Array.filter (fun x -> not (x.Contains boundary))
              |> Array.map (fun x ->
                   Regex.Split(x, "^Content-Transfer-Encoding: binary", RegexOptions.Multiline ||| RegexOptions.IgnoreCase)
                   |> Array.skip 1
                   |> Array.head)

            let httpRequests =
              rawRequests
              |> Array.map (HttpRequest.parse)
              |> List.ofArray

            match httpRequests |> List.forall Result.isOk with
            | true -> httpRequests |> List.map Result.valueOf |> Some
            | false -> None
        | _ -> None
    | _ -> None

  let rec toCommand =
    function
    | ListTablesRequest -> Table ListTables |> Some
    | CreateTableRequest name -> CreateTable name |> Table |> Some
    | InsertOrMergeRequest (table, partitionKey, rowKey, fields) ->
        InsertOrMerge
          (table,
           { Keys =
               { PartitionKey = partitionKey
                 RowKey = rowKey }
             Fields = (fields |> TableFields.fromJObject) })
        |> Write
        |> Some
    | InsertOrReplaceRequest (table, partitionKey, rowKey, fields) ->
        InsertOrReplace
          (table,
           { Keys =
               { PartitionKey = partitionKey
                 RowKey = rowKey }
             Fields = (fields |> TableFields.fromJObject) })
        |> Write
        |> Some
    | InsertRequest (table, partitionKey, rowKey, fields) ->
        Insert
          (table,
           { Keys =
               { PartitionKey = partitionKey
                 RowKey = rowKey }
             Fields = (fields |> TableFields.fromJObject) })
        |> Write
        |> Some
    | ReplaceRequest (table, partitionKey, rowKey, etag, fields) ->
        Replace
          (table,
           etag,
           { Keys =
               { PartitionKey = partitionKey
                 RowKey = rowKey }
             Fields = (fields |> TableFields.fromJObject) })
        |> Write
        |> Some
    | MergeRequest (table, partitionKey, rowKey, etag, fields) ->
        Merge
          (table,
           etag,
           { Keys =
               { PartitionKey = partitionKey
                 RowKey = rowKey }
             Fields = (fields |> TableFields.fromJObject) })
        |> Write
        |> Some
    | DeleteRequest (table, partitionKey, rowKey) ->
        Delete
          (table,
           { PartitionKey = partitionKey
             RowKey = rowKey })
        |> Write
        |> Some
    | GetRequest (table, partitionKey, rowKey) ->
        Get
          (table,
           { PartitionKey = partitionKey
             RowKey = rowKey })
        |> Read
        |> Some
    | QueryRequest request -> Query request |> Read |> Some
    | BatchRequest requests ->
        let commands = requests |> List.map (toCommand)
        match commands |> List.forall (Option.isSome) with
        | true ->
            { BatchCommand.Commands =
                commands
                |> List.map (Option.valueOf)
                |> List.map (WriteCommand.valueOf) }
            |> Batch
            |> Some
        | _ -> None

    | request ->
        printfn "Unknown request %A" request
        None

let exceptionLoggingHttpHandler (inner: HttpContext -> Task) (ctx: HttpContext) =
  task {
    try
      do! inner ctx
    with ex ->
      printfn "Ouch %A" ex
      ctx.Response.StatusCode <- 500
  } :> Task

let httpHandler commandHandler (ctx: HttpContext) =
  ctx.Request
  |> HttpRequest.toRequest
  |> Request.toCommand
  |> Option.map
       (commandHandler
        >> HttpResponse.fromCommandResponse
        >> HttpResponse.applyToCtx ctx)
  |> Option.defaultWith (HttpResponse.statusCode ctx StatusCode.NotFound) :> Task
