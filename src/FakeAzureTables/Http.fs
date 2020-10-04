module Http

open Newtonsoft.Json.Linq
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks
open System.Threading.Tasks
open System.IO
open System.Text.RegularExpressions
open Domain

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

let private (|QueryEntity|_|) (request: HttpRequest) =
  match request.Path.Value with
  | Regex "^\/devstoreaccount1\/(\w+)$" [ tableName ] ->
      match request.Query.ContainsKey("$filter") with
      | true -> Some(tableName, request.Query.Item "$filter" |> string)
      | false -> None
  | _ -> None

let private (|CreateTable|_|) (request: HttpRequest) =
  match request.Path.Value with
  | "/devstoreaccount1/Tables()" ->
      let jObject = JObject.Parse request.BodyString
      match jObject.TryGetValue "TableName" with
      | true, tableName -> Some (CreateTable(string tableName))
      | _ -> None
  | _ -> None


let private (|InsertEntity|_|) (request: HttpRequest) =
  match request.Path.Value with
  | Regex "^\/devstoreaccount1\/(\w+)\(\)$" [ tableName ] ->
      match request.Method with
      | "POST" ->
          let jObject = JObject.Parse request.BodyString
          match jObject.TryGetValue "PartitionKey" with
          | true, p ->
              match jObject.TryGetValue "RowKey" with
              | true, r -> Some(InsertEntity(tableName, string p, string r, jObject))
              | _ -> None
          | _ -> None
      | _ -> None
  | _ -> None

let private (|InsertOrMergeEntity|InsertOrReplaceEntity|DeleteEntity|GetEntity|NotFound|) (request: HttpRequest) =
  match request.Path.Value with
  | Regex "^\/devstoreaccount1\/(\w+)\(PartitionKey='(.+)',RowKey='(.+)'\)$" [ tableName; p; r ] ->
      match request.Method with
      | "POST" ->
          let jObject = JObject.Parse request.BodyString
          InsertOrMergeEntity(tableName, p, r, jObject)
      | "PUT" ->
          let jObject = JObject.Parse request.BodyString
          InsertOrReplaceEntity(tableName, p, r, jObject)
      | "GET" -> GetEntity(tableName, p, r)
      | "DELETE" -> DeleteEntity(tableName, p, r)
      | _ -> NotFound
  | _ -> NotFound

let exceptonLoggingHttpHandler (inner: HttpContext -> Task) (ctx: HttpContext) =
  task {
    try
      do! inner ctx
    with ex -> printfn "Ouch %A" ex
  } :> Task

let httpHandler commandHandler (ctx: HttpContext) =
  task {
    match ctx.Request with
    | CreateTable name ->
        match CreateTable name |> commandHandler with
        | Ack -> ctx.Response.StatusCode <- 204
        | Conflict _ -> ctx.Response.StatusCode <- 409
        | _ -> ctx.Response.StatusCode <- 500
    | InsertOrMergeEntity (table, partitionKey, rowKey, fields) ->
        match InsertOrMerge
                (table,
                 ({ PartitonKey = partitionKey
                    RowKey = rowKey },
                  fields |> TableFields.fromJObject))
              |> commandHandler with
        | Ack -> ctx.Response.StatusCode <- 204
        | _ -> ctx.Response.StatusCode <- 500
        ctx.Response.StatusCode <- 204
    | InsertOrReplaceEntity (table, partitionKey, rowKey, fields) ->
        match InsertOrMerge
                (table,
                 ({ PartitonKey = partitionKey
                    RowKey = rowKey },
                  fields |> TableFields.fromJObject))
              |> commandHandler with
        | Ack -> ctx.Response.StatusCode <- 204
        | _ -> ctx.Response.StatusCode <- 500
        ctx.Response.StatusCode <- 204
    | InsertEntity (table, partitionKey, rowKey, fields) ->
        match Insert
                (table,
                 ({ PartitonKey = partitionKey
                    RowKey = rowKey },
                  fields |> TableFields.fromJObject))
              |> commandHandler with
        | Ack -> ctx.Response.StatusCode <- 204
        | Conflict _ -> ctx.Response.StatusCode <- 409
        | _ -> ctx.Response.StatusCode <- 500
    | DeleteEntity (table, partitionKey, rowKey) ->
        Delete
          (table,
           { PartitonKey = partitionKey
             RowKey = rowKey })
        |> commandHandler
        |> ignore
        ctx.Response.StatusCode <- 204
    | GetEntity (table, partitionKey, rowKey) ->
        match Get
                (table,
                 { PartitonKey = partitionKey
                   RowKey = rowKey })
              |> commandHandler with
        | GetResponse entity ->
            ctx.Response.StatusCode <- 200
            ctx.Response.ContentType <- "application/json; charset=utf-8"
            let jObject = entity |> TableRow.toJObject
            let json = jObject.ToString()
            do! json |> ctx.Response.WriteAsync
        | CommandResult.NotFound -> ctx.Response.StatusCode <- 404
        | _ -> ctx.Response.StatusCode <- 500
    | QueryEntity request ->
        match Query request |> commandHandler with
        | QueryResponse results ->
            ctx.Response.StatusCode <- 200
            ctx.Response.ContentType <- "application/json; charset=utf-8"

            let rows =
              results |> List.map (TableRow.toJObject) |> JArray

            let response = JObject([ JProperty("value", rows) ])
            let json = (response.ToString())
            do! json |> ctx.Response.WriteAsync
        | _ ->

            ctx.Response.StatusCode <- 404
    | _ ->
        printfn "Bugger %A:%A:%A" ctx.Request.BodyString ctx.Request.Method ctx.Request.QueryString.Value
        ctx.Response.StatusCode <- 404
  } :> Task