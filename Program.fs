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
open FilterParser.Parser
open Domain

let rec applyFilter (rows: TableRow seq) filter =

  let compareFields left qc right =

    let fieldsEqual (left, right) = left = right

    let fieldsGreaterThan (left, right) =
      match left, right with
      | FieldValue.Int left, FieldValue.Int right -> left > right
      | FieldValue.Long left, FieldValue.Long right -> left > right
      | FieldValue.Date left, FieldValue.Date right -> left > right
      | FieldValue.Guid left, FieldValue.Guid right -> left > right
      | FieldValue.Binary left, FieldValue.Binary right -> left > right
      | FieldValue.Bool left, FieldValue.Bool right -> left > right
      | FieldValue.Double left, FieldValue.Double right -> left > right
      | _, _ -> left.AsString > right.AsString

    let fieldsGreaterThanOrEqual (left, right) =
      match left, right with
      | FieldValue.Int left, FieldValue.Int right -> left >= right
      | FieldValue.Long left, FieldValue.Long right -> left >= right
      | FieldValue.Date left, FieldValue.Date right -> left >= right
      | FieldValue.Guid left, FieldValue.Guid right -> left >= right
      | FieldValue.Binary left, FieldValue.Binary right -> left >= right
      | FieldValue.Bool left, FieldValue.Bool right -> left >= right
      | FieldValue.Double left, FieldValue.Double right -> left >= right
      | _, _ -> left.AsString >= right.AsString

    let fieldsLessThan (left, right) =
      match left, right with
      | FieldValue.Int left, FieldValue.Int right -> left < right
      | FieldValue.Long left, FieldValue.Long right -> left < right
      | FieldValue.Date left, FieldValue.Date right -> left < right
      | FieldValue.Guid left, FieldValue.Guid right -> left < right
      | FieldValue.Binary left, FieldValue.Binary right -> left < right
      | FieldValue.Bool left, FieldValue.Bool right -> left < right
      | FieldValue.Double left, FieldValue.Double right -> left < right
      | _, _ -> left.AsString < right.AsString

    let fieldsLessThanOrEqual (left, right) =
      match left, right with
      | FieldValue.Int left, FieldValue.Int right -> left <= right
      | FieldValue.Long left, FieldValue.Long right -> left <= right
      | FieldValue.Date left, FieldValue.Date right -> left <= right
      | FieldValue.Guid left, FieldValue.Guid right -> left <= right
      | FieldValue.Binary left, FieldValue.Binary right -> left <= right
      | FieldValue.Bool left, FieldValue.Bool right -> left <= right
      | FieldValue.Double left, FieldValue.Double right -> left <= right
      | _, _ -> left.AsString <= right.AsString

    match qc with
    | QueryComparison.Equal -> fieldsEqual (left, right)
    | QueryComparison.NotEqual -> fieldsEqual (left, right) |> not
    | QueryComparison.GreaterThan -> fieldsGreaterThan (left, right)
    | QueryComparison.GreaterThanOrEqual -> fieldsGreaterThanOrEqual (left, right)
    | QueryComparison.LessThan -> fieldsLessThan (left, right)
    | QueryComparison.LessThanOrEqual -> fieldsLessThanOrEqual (left, right)

  match filter with
  | Filter.PartionKey(qc, pk) ->
      rows |> Seq.filter (fun r -> compareFields (FieldValue.String r.PartitonKey) qc (FieldValue.String pk))
  | Filter.RowKey(qc, rk) ->
      rows |> Seq.filter (fun r -> compareFields (FieldValue.String r.RowKey) qc (FieldValue.String rk))
  | Filter.Property(name, qc, value) ->
      rows
      |> Seq.filter (fun r ->
           r.Fields
           |> List.tryFind (fun f -> f.Name = name)
           |> function
           | Some field -> (compareFields field.Value qc value)
           | _ -> false)
  | Filter.Combined(left, tableOperator, right) ->
      match tableOperator with
      | TableOperators.And ->
          let results = applyFilter rows left
          applyFilter results right
      | TableOperators.Or ->
          let leftResults = applyFilter rows left |> Set.ofSeq
          let rightResults = applyFilter rows right |> Set.ofSeq
          leftResults
          |> Set.union rightResults
          |> Set.toSeq

let commandHandler (tables: Tables) command =
  match command with
  | CreateTable name ->
      tables.Add(name, ResizeArray())
      Ack
  | InsertOrMerge(table, row) ->
      let table = tables.[table]
      table.Add row
      Ack
  | Insert(table, row) ->
      let table = tables.[table]
      table.Add row
      Ack
  | Get(table, partitionKey, rowKey) ->
      let table = tables.[table]
      match table |> Seq.tryFind (fun x -> x.PartitonKey = partitionKey && x.RowKey = rowKey) with
      | Some row -> GetResponse row
      | _ -> NotFound
  | Query(table, filter) ->
      let table = tables.[table]

      let matchingRows =
        match fParse filter with
        | Result.Ok result -> applyFilter table result
        | Result.Error error ->
            printfn "Filter: %A;\nError: %A" filter error
            Seq.empty
      matchingRows
      |> Seq.toList
      |> QueryResponse
  | Delete(table, partitionKey, rowKey) ->
      let table = tables.[table]
      table.RemoveAll(fun x -> x.PartitonKey = partitionKey && x.RowKey = rowKey) |> ignore
      Ack

type HttpRequest with
  member __.BodyString = (new StreamReader(__.Body)).ReadToEnd()

let private findPort() =
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

let (|QueryEntity|_|) (request: HttpRequest) =
  match request.Path.Value with
  | Regex "^\/devstoreaccount1\/(\w+)$" [ tableName ] ->
      match request.Query.ContainsKey("$filter") with
      | true -> Some(tableName, request.Query.Item "$filter" |> string)
      | false -> None
  | _ -> None

let (|CreateTable|InsertEntity|InsertOrMergeEntity|InsertOrReplaceEntity|DeleteEntity|GetEntity|NotFound|) (request: HttpRequest)
  =
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


let handler commandHandler (ctx: HttpContext) =
  task {
    match ctx.Request with
    | CreateTable name ->
        CreateTable name
        |> commandHandler
        |> ignore
        ctx.Response.StatusCode <- 204
    | InsertOrMergeEntity(table, partitionKey, rowKey, fields) ->
        InsertOrMerge
          (table,
           { PartitonKey = partitionKey
             RowKey = rowKey
             Fields = fields |> TableFields.fromJObject })
        |> commandHandler
        |> ignore
        ctx.Response.StatusCode <- 204
    | InsertOrReplaceEntity(table, partitionKey, rowKey, fields) ->
        InsertOrMerge
          (table,
           { PartitonKey = partitionKey
             RowKey = rowKey
             Fields = fields |> TableFields.fromJObject })
        |> commandHandler
        |> ignore
        ctx.Response.StatusCode <- 204
    | InsertEntity(table, partitionKey, rowKey, fields) ->
        Insert
          (table,
           { PartitonKey = partitionKey
             RowKey = rowKey
             Fields = fields |> TableFields.fromJObject })
        |> commandHandler
        |> ignore
        ctx.Response.StatusCode <- 204
    | DeleteEntity request ->
        Delete request
        |> commandHandler
        |> ignore
        ctx.Response.StatusCode <- 204
    | GetEntity request ->
        match Get request |> commandHandler with
        | GetResponse entity ->
            ctx.Response.StatusCode <- 200
            ctx.Response.ContentType <- "application/json; charset=utf-8"
            let json = entity.ToString()
            do! json |> ctx.Response.WriteAsync
        | _ -> ctx.Response.StatusCode <- 404
    | QueryEntity request ->
        match Query request |> commandHandler with
        | QueryResponse results ->
            ctx.Response.StatusCode <- 200
            ctx.Response.ContentType <- "application/json; charset=utf-8"
            let rows =
              results
              |> List.map (TableRow.toJObject)
              |> JArray

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
  appBuilder.Run(fun ctx -> handler (commandHandler tables) ctx)

type FakeTables() =
  let tables = Dictionary<string, ResizeArray<_>>()
  let port = findPort()
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
  member __.Client = CloudStorageAccount.Parse(connectionString).CreateCloudTableClient()
  member __.Tables = tables
  interface IDisposable with
    member __.Dispose() = webHost.Dispose()


[<EntryPoint>]
let main argv =
  let tables = new FakeTables()
  let table = tables.Client.GetTableReference "test"
  table.CreateIfNotExists() |> ignore
  let fields =
    [ ("StringField", EntityProperty.GeneratePropertyForString("StringValue"))
      ("DateField", EntityProperty.GeneratePropertyForDateTimeOffset(Nullable DateTimeOffset.UtcNow))
      ("IntField", EntityProperty.GeneratePropertyForInt(Nullable 1))
      ("LongField", EntityProperty.GeneratePropertyForLong(Nullable 1L))
      ("GuidField", EntityProperty.GeneratePropertyForGuid(Nullable(Guid.NewGuid())))
      ("FloatField", EntityProperty.GeneratePropertyForDouble(Nullable 1.))
      ("BoolField", EntityProperty.GeneratePropertyForBool(Nullable true))
      ("ByteArrayField", EntityProperty.GeneratePropertyForByteArray([| 104uy; 101uy; 108uy; 108uy; 111uy |])) ]
    |> dict

  DynamicTableEntity("pk", "rk", "*", fields)
  |> TableOperation.InsertOrReplace
  |> table.Execute
  |> ignore

  let fields2 =
    [ ("StringField", EntityProperty.GeneratePropertyForString("StringValue2"))
      ("DateField", EntityProperty.GeneratePropertyForDateTimeOffset(Nullable DateTimeOffset.UtcNow))
      ("IntField", EntityProperty.GeneratePropertyForInt(Nullable 2))
      ("LongField", EntityProperty.GeneratePropertyForLong(Nullable 2L))
      ("GuidField", EntityProperty.GeneratePropertyForGuid(Nullable(Guid.NewGuid())))
      ("FloatField", EntityProperty.GeneratePropertyForDouble(Nullable 2.))
      ("BoolField", EntityProperty.GeneratePropertyForBool(Nullable false))
      ("ByteArrayField", EntityProperty.GeneratePropertyForByteArray([| 104uy; 101uy; 108uy; 108uy; 111uy |])) ]
    |> dict

  DynamicTableEntity("pk2", "r2k", "*", fields2)
  |> TableOperation.InsertOrReplace
  |> table.Execute
  |> ignore

  // let a = TableQuery.GenerateFilterConditionForDate("Field1", QueryComparisons.GreaterThanOrEqual, DateTimeOffset.UtcNow)
  // let b = TableQuery.GenerateFilterConditionForDate("Field2", QueryComparisons.GreaterThanOrEqual, DateTimeOffset.UtcNow)
  // let rowkey = TableQuery.GenerateFilterCondition("rowkey", QueryComparisons.Equal, "value")
  // let combined = TableQuery.CombineFilters(a, TableOperators.And, b)
  // let combined2 = TableQuery.CombineFilters(rowkey, TableOperators.And, combined)
  // let query = TableQuery<DynamicTableEntity>().Where combined2
  // let filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "pk")
  // let filter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "rk")
  let left = TableQuery.GenerateFilterCondition("StringField", QueryComparisons.Equal, "StringValue2")
  let right = TableQuery.GenerateFilterConditionForLong("LongField", QueryComparisons.GreaterThanOrEqual, 2L)
  let filter = TableQuery.CombineFilters(left, Microsoft.Azure.Cosmos.Table.TableOperators.And, right)
  let query = TableQuery<DynamicTableEntity>().Where filter
  let token = TableContinuationToken()
  let results = table.ExecuteQuerySegmentedAsync(query, token).Result
  for result in results do
    printfn "PartitionKey is %A" result.PartitionKey
    printfn "RowKey is %A" result.RowKey
    printfn "Properties are %A" (result.Properties |> List.ofSeq)

  0
