module HttpContext

open Newtonsoft.Json.Linq
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks
open System.Threading.Tasks
open System.Text.RegularExpressions
open Domain

module private Request =
  open Http

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
    match request.Path with
    | Regex "^\/devstoreaccount1\/(\w+)$" [ tableName ] ->
        match request.Query.ContainsKey("$filter") with
        | true -> Some(tableName, request.Query.Item "$filter" |> Array.head)
        | false -> None
    | _ -> None

  let private (|CreateTableRequest|_|) (request: Request) =
    match request.Path with
    | "/devstoreaccount1/Tables()" ->
        let jObject = JObject.Parse request.Body
        match jObject.TryGetValue "TableName" with
        | true, tableName -> Some(string tableName)
        | _ -> None
    | _ -> None


  let private (|InsertRequest|_|) (request: Request) =
    match request.Path with
    | Regex "^\/devstoreaccount1\/(\w+)\(\)$" [ tableName ] ->
        match request.Method with
        | Method.POST ->
            let jObject = JObject.Parse request.Body
            match jObject.TryGetValue "PartitionKey" with
            | true, p ->
                match jObject.TryGetValue "RowKey" with
                | true, r -> Some(tableName, string p, string r, jObject)
                | _ -> None
            | _ -> None
        | _ -> None
    | _ -> None

  let private (|InsertOrMergeRequest|InsertOrReplaceRequest|DeleteRequest|GetRequest|NotFoundRequest|) (request: Request) =
    match request.Path with
    | Regex "^\/devstoreaccount1\/(\w+)\(PartitionKey='(.+)',RowKey='(.+)'\)$" [ tableName; p; r ] ->
        match request.Method with
        | Method.POST ->
            let jObject = JObject.Parse request.Body
            InsertOrMergeRequest(tableName, p, r, jObject)
        | Method.PUT ->
            let jObject = JObject.Parse request.Body
            InsertOrReplaceRequest(tableName, p, r, jObject)
        | Method.GET -> GetRequest(tableName, p, r)
        | Method.DELETE -> DeleteRequest(tableName, p, r)
        | _ -> NotFoundRequest
    | _ -> NotFoundRequest

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

  let private (|BatchRequest|_|) (request: Request) =
    match request.Path with
    | "/devstoreaccount1/$batch" ->
        match (request.Headers.Item "Content-Type")
              |> Array.head with
        | Regex "boundary=(.+)$" [ boundary ] ->
            let rawRequests =
              Regex.Split(request.Body, "--changeset_.+$", RegexOptions.Multiline)
              |> Array.filter (fun x -> not (x.Contains boundary))
              |> Array.map (fun x ->
                   Regex.Split(x, "^Content-Transfer-Encoding: binary", RegexOptions.Multiline)
                   |> Array.skip 1
                   |> Array.head)

            let httpRequests =
              rawRequests
              |> Array.map (Parser.parse)
              |> List.ofArray

            match httpRequests |> List.forall Result.isOk with
            | true -> httpRequests |> List.map Result.valueOf |> Some
            | false -> None
        | _ -> None
    | _ -> None

  let rec toCommand =
    function
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
    with ex -> printfn "Ouch %A" ex
  } :> Task

let httpHandler commandHandler (ctx: HttpContext) =
  task {
    match ctx.Request |> Http.toRequest |> Request.toCommand with
    | Some command ->
        let commandResult = commandHandler command
        match commandResult with
        | TableResponse tableResponse ->
          match tableResponse with
          | TableCommandResponse.Ack -> ctx.Response.StatusCode <- 204
          | TableCommandResponse.Conflict _ -> ctx.Response.StatusCode <- 409
        | WriteResponse writeResponse ->
          match writeResponse with
          | Ack -> ctx.Response.StatusCode <- 204
          | Conflict _ -> ctx.Response.StatusCode <- 409
        | ReadResponse readResponse ->
          match readResponse with
          | GetResponse response ->
            ctx.Response.StatusCode <- 200
            ctx.Response.ContentType <- "application/json; charset=utf-8"
            let jObject = response |> TableRow.toJObject
            let json = jObject.ToString()
            do! json |> ctx.Response.WriteAsync
          | QueryResponse results ->
              ctx.Response.StatusCode <- 200
              ctx.Response.ContentType <- "application/json; charset=utf-8"

              let rows =
                results |> List.map (TableRow.toJObject) |> JArray

              let response = JObject([ JProperty("value", rows) ])
              let json = (response.ToString())
              do! json |> ctx.Response.WriteAsync
          | ReadCommandResponse.NotFoundResponse ->
              ctx.Response.StatusCode <- 404
        | BatchResponse commandResults -> ctx.Response.StatusCode <- 202
        | NotFoundResponse -> ctx.Response.StatusCode <- 404
    | None -> ctx.Response.StatusCode <- 400
  } :> Task
