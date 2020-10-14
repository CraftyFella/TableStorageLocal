module Http

open Newtonsoft.Json.Linq
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks
open System.Threading.Tasks
open System.IO
open System.Text.RegularExpressions
open Domain

module private Request =

  type HttpRequest with
    member __.BodyString = (new StreamReader(__.Body)).ReadToEnd()

  let (|Regex|_|) pattern input =
    match Regex.Match(input, pattern) with
    | m when m.Success ->
        m.Groups
        |> Seq.skip 1
        |> Seq.map (fun g -> g.Value)
        |> Seq.toList
        |> Some
    | _ -> None

  let private (|QueryRequest|_|) (request: HttpRequest) =
    match request.Path.Value with
    | Regex "^\/devstoreaccount1\/(\w+)$" [ tableName ] ->
        match request.Query.ContainsKey("$filter") with
        | true -> Some(tableName, request.Query.Item "$filter" |> string)
        | false -> None
    | _ -> None

  let private (|CreateTableRequest|_|) (request: HttpRequest) =
    match request.Path.Value with
    | "/devstoreaccount1/Tables()" ->
        let jObject = JObject.Parse request.BodyString
        match jObject.TryGetValue "TableName" with
        | true, tableName -> Some(string tableName)
        | _ -> None
    | _ -> None


  let private (|InsertRequest|_|) (request: HttpRequest) =
    match request.Path.Value with
    | Regex "^\/devstoreaccount1\/(\w+)\(\)$" [ tableName ] ->
        match request.Method with
        | "POST" ->
            let jObject = JObject.Parse request.BodyString
            match jObject.TryGetValue "PartitionKey" with
            | true, p ->
                match jObject.TryGetValue "RowKey" with
                | true, r -> Some(tableName, string p, string r, jObject)
                | _ -> None
            | _ -> None
        | _ -> None
    | _ -> None

  let private (|InsertOrMergeRequest|InsertOrReplaceRequest|DeleteRequest|GetRequest|NotFoundRequest|) (request: HttpRequest) =
    match request.Path.Value with
    | Regex "^\/devstoreaccount1\/(\w+)\(PartitionKey='(.+)',RowKey='(.+)'\)$" [ tableName; p; r ] ->
        match request.Method with
        | "POST" ->
            let jObject = JObject.Parse request.BodyString
            InsertOrMergeRequest(tableName, p, r, jObject)
        | "PUT" ->
            let jObject = JObject.Parse request.BodyString
            InsertOrReplaceRequest(tableName, p, r, jObject)
        | "GET" -> GetRequest(tableName, p, r)
        | "DELETE" -> DeleteRequest(tableName, p, r)
        | _ -> NotFoundRequest
    | _ -> NotFoundRequest

  let toCommand =
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
    | _ -> None

let exceptonLoggingHttpHandler (inner: HttpContext -> Task) (ctx: HttpContext) =
  task {
    try
      do! inner ctx
    with ex -> printfn "Ouch %A" ex
  } :> Task

let httpHandler commandHandler (ctx: HttpContext) =
  task {
    match ctx.Request |> Request.toCommand with
    | Some command ->
        let response = commandHandler command
        match response with
        | Ack -> ctx.Response.StatusCode <- 204
        | Conflict _ -> ctx.Response.StatusCode <- 409
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
        | NotFound -> ctx.Response.StatusCode <- 404
    | None -> ctx.Response.StatusCode <- 400
  } :> Task
