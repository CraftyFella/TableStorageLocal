module FakeTableStorage

open System
open Microsoft.Azure.Cosmos.Table
open System.Collections.Generic
open Newtonsoft.Json.Linq
open Microsoft.AspNetCore.Hosting
open System.Net.Sockets
open System.Net
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks
open System.Threading.Tasks
open System.IO
open System.Text.RegularExpressions
open FilterParser
open FilterApplier
open Domain

let commandHandler (tables: Tables) command =
  match command with
  | CreateTable name ->
      match tables.TryAdd(name, Dictionary()) with
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
      printfn "Filter: %A" filter

      let matchingRows =
        match fParse filter with
        | Result.Ok result -> applyFilter table result
        | Result.Error error ->
            printfn "Filter: %A;\nError: %A" filter error
            Seq.empty

      matchingRows |> Seq.toList |> QueryResponse
  | Delete (table, keys) ->
      let table = tables.[table]
      table.Remove(keys) |> ignore
      Ack

type HttpRequest with
  member __.BodyString = (new StreamReader(__.Body)).ReadToEnd()

let private findPort () =
  TcpListener(IPAddress.Loopback, 0)
  |> fun l ->
       l.Start()
       (l, (l.LocalEndpoint :?> IPEndPoint).Port)
       |> fun (l, p) ->
            l.Stop()
            p

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

let private (|CreateTable|InsertEntity|InsertOrMergeEntity|InsertOrReplaceEntity|DeleteEntity|GetEntity|NotFound|) (request: HttpRequest) =
  match request.Path.Value with
  | "/devstoreaccount1/Tables()" ->
      let jObject = JObject.Parse request.BodyString
      match jObject.TryGetValue "TableName" with
      | true, tableName -> CreateTable(string tableName)
      | _ -> NotFound // TODO Replace with bad request
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
  | Regex "^\/devstoreaccount1\/(\w+)\(\)$" [ tableName ] ->
      match request.Method with
      | "POST" ->
          let jObject = JObject.Parse request.BodyString
          match jObject.TryGetValue "PartitionKey" with
          | true, p ->
              match jObject.TryGetValue "RowKey" with
              | true, r -> InsertEntity(tableName, string p, string r, jObject)
              | _ -> NotFound
          | _ -> NotFound
      | _ -> NotFound
  | _ -> NotFound

let exceptonLoggingHandler (inner: HttpContext -> Task) (ctx: HttpContext) =
  task {
    try
      do! inner ctx
    with ex -> printfn "Ouch %A" ex
  } :> Task


let handler commandHandler (ctx: HttpContext) =
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

let private app tables (appBuilder: IApplicationBuilder) =
  let inner = handler (commandHandler tables)
  appBuilder.Run(fun ctx -> exceptonLoggingHandler inner ctx)

type FakeTables() =
  let tables = Dictionary<string, _>()
  let port = findPort ()
  // let port = 10002
  let url = sprintf "http://127.0.0.1:%i" port

  let connectionString =
    sprintf
      "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;TableEndpoint=http://localhost.charlesproxy.com:%i/devstoreaccount1;"
      port

  let webHost =
    WebHostBuilder().Configure(fun appBuilder -> app tables appBuilder).UseUrls(url)
      .UseKestrel(fun options -> options.AllowSynchronousIO <- true).Build()

  do webHost.Start()

  member __.Client =
    CloudStorageAccount.Parse(connectionString).CreateCloudTableClient()

  member __.Tables = tables

  interface IDisposable with
    member __.Dispose() = webHost.Dispose()
