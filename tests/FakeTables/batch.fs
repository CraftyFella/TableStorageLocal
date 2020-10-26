module batch

open Expecto
open Microsoft.Azure.Cosmos.Table

[<Tests>]
let batchMergeTests =
  testList
    "batch merge"
    [ test "merge existing row is accepted" {


        let table = createFakeTables ()

        let entity =
          DynamicTableEntity("pk2", "r2k", null, stringFieldType "Inserted Value")

        entity
        |> TableOperation.Insert
        |> table.Execute
        |> ignore

        let batch = TableBatchOperation()

        let mergedEntity =
          DynamicTableEntity("pk2", "r2k", entity.ETag, stringFieldType "Updated Value")

        mergedEntity |> TableOperation.Merge |> batch.Add

        let actual = batch |> table.ExecuteBatch

        Expect.equal (actual |> Seq.length) 1 "unexpected result"
        for batchItem in actual do
          Expect.equal (batchItem.HttpStatusCode) 204 "unexpected result"
      } ]

[<Tests>]
let batchDeleteTests =
  testList
    "batch delete"
    [ test "delete existing row is accepted" {
        let table = createFakeTables ()

        let entity =
          DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())

        let insertResult =
          entity |> TableOperation.Insert |> table.Execute

        let batch = TableBatchOperation()

        entity |> TableOperation.Delete |> batch.Add

        let batchResult = batch |> table.ExecuteBatch

        Expect.equal (batchResult |> Seq.length) 1 "unexpected result"
        for batchItem in batchResult do
          Expect.equal (batchItem.HttpStatusCode) 204 "unexpected result"
      } ]

[<Tests>]
let batchInsertTests =
  testList
    "batch insert"
    [ test "row doesn't exist is accepted" {
        let table = createFakeTables ()
        let batch = TableBatchOperation()

        createEntityWithString "pk2" "5" "thing"
        |> TableOperation.Insert
        |> batch.Add

        createEntityWithString "pk2" "6" "thing"
        |> TableOperation.Insert
        |> batch.Add

        let actual = batch |> table.ExecuteBatch

        Expect.equal (actual |> Seq.length) 2 "unexpected result"
        for batchItem in actual do
          Expect.equal (batchItem.HttpStatusCode) 204 "unexpected result"

      }

      test "2 batches in a row" {
        let table = createFakeTables ()
        let batch = TableBatchOperation()

        createEntityWithString "pk" "1" "rowValue"
        |> TableOperation.Insert
        |> batch.Add

        let actual = batch |> table.ExecuteBatch

        Expect.equal (actual |> Seq.length) 1 "unexpected result"
        for batchItem in actual do
          Expect.equal (batchItem.HttpStatusCode) 204 "unexpected result"

        let batch = TableBatchOperation()
        createEntityWithString "pk" "2" "rowValue"
        |> TableOperation.Insert
        |> batch.Add
        let actual = batch |> table.ExecuteBatch

        Expect.equal (actual |> Seq.length) 1 "unexpected result"
        for batchItem in actual do
          Expect.equal (batchItem.HttpStatusCode) 204 "unexpected result"

      }

      ptest "multiple changes with same row key" {

        (*
          HTTP/1.1 202 Accepted
          Cache-Control: no-cache
          Transfer-Encoding: chunked
          Content-Type: multipart/mixed; boundary=batchresponse_c8b55e8d-9f3e-4173-baaf-d815fde35c74
          Server: Windows-Azure-Table/1.0 Microsoft-HTTPAPI/2.0
          x-ms-request-id: 0b0b72c3-4002-0033-44b5-abf168000000
          x-ms-version: 2017-07-29
          X-Content-Type-Options: nosniff
          Date: Mon, 26 Oct 2020 16:31:21 GMT
          Connection: keep-alive

          --batchresponse_c8b55e8d-9f3e-4173-baaf-d815fde35c74
          Content-Type: multipart/mixed; boundary=changesetresponse_1515a9c3-1172-4d7d-aa1c-9ba1019da799

          --changesetresponse_1515a9c3-1172-4d7d-aa1c-9ba1019da799
          Content-Type: application/http
          Content-Transfer-Encoding: binary

          HTTP/1.1 400 Bad Request
          X-Content-Type-Options: nosniff
          Cache-Control: no-cache
          Preference-Applied: return-no-content
          DataServiceVersion: 3.0;
          Content-Type: application/json;odata=minimalmetadata;streaming=true;charset=utf-8

          {"odata.error":{"code":"InvalidDuplicateRow","message":{"lang":"en-US","value":"1:The batch request contains multiple changes with same row key. An entity can appear only once in a batch request.\nRequestId:0b0b72c3-4002-0033-44b5-abf168000000\nTime:2020-10-26T16:31:22.2128155Z"}}}
          --changesetresponse_1515a9c3-1172-4d7d-aa1c-9ba1019da799--
          --batchresponse_c8b55e8d-9f3e-4173-baaf-d815fde35c74--
        *)

        let table = createFakeTables ()
        let batch = TableBatchOperation()

        createEntityWithString "pk" "1" "rowValue"
        |> TableOperation.Insert
        |> batch.Add

        createEntityWithString "pk" "1" "rowValue"
        |> TableOperation.Insert
        |> batch.Add

        let actual = batch |> table.ExecuteBatch

        Expect.equal (actual |> Seq.length) 2 "unexpected result"
        for batchItem in actual do
          Expect.equal (batchItem.HttpStatusCode) 204 "unexpected result"

      }

      ptest "batch tries to insert a row which already exists" {

        (*

          HTTP/1.1 202 Accepted
          Cache-Control: no-cache
          Transfer-Encoding: chunked
          Content-Type: multipart/mixed; boundary=batchresponse_b6f890b8-8144-46d3-8574-f09db410f594
          Server: Windows-Azure-Table/1.0 Microsoft-HTTPAPI/2.0
          x-ms-request-id: 8c9d6308-c002-0029-50b6-abde07000000
          x-ms-version: 2017-07-29
          X-Content-Type-Options: nosniff
          Date: Mon, 26 Oct 2020 16:41:37 GMT
          Connection: keep-alive

          --batchresponse_b6f890b8-8144-46d3-8574-f09db410f594
          Content-Type: multipart/mixed; boundary=changesetresponse_36817914-a3a0-43bb-9d66-8d670962fa3a

          --changesetresponse_36817914-a3a0-43bb-9d66-8d670962fa3a
          Content-Type: application/http
          Content-Transfer-Encoding: binary

          HTTP/1.1 409 Conflict
          X-Content-Type-Options: nosniff
          Cache-Control: no-cache
          Preference-Applied: return-no-content
          DataServiceVersion: 3.0;
          Content-Type: application/json;odata=minimalmetadata;streaming=true;charset=utf-8

          {"odata.error":{"code":"EntityAlreadyExists","message":{"lang":"en-US","value":"1:The specified entity already exists.\nRequestId:8c9d6308-c002-0029-50b6-abde07000000\nTime:2020-10-26T16:41:38.2990653Z"}}}
          --changesetresponse_36817914-a3a0-43bb-9d66-8d670962fa3a--
          --batchresponse_b6f890b8-8144-46d3-8574-f09db410f594--
        *)

        let table = createFakeTables ()
        DynamicTableEntity("pk", "2", "*", allFieldTypes ())
        |> TableOperation.Insert
        |> table.Execute
        |> ignore

        let batch = TableBatchOperation()

        createEntityWithString "pk" "1" "rowValue"
        |> TableOperation.Insert
        |> batch.Add

        createEntityWithString "pk" "2" "rowValue"
        |> TableOperation.Insert
        |> batch.Add

        // TODO: Blows up here
        let actual = batch |> table.ExecuteBatch

        Expect.equal (actual |> Seq.length) 2 "unexpected result"
        for batchItem in actual do
          Expect.equal (batchItem.HttpStatusCode) 204 "unexpected result"

      }

      test "inserted rows are retrievable" {
        let table = createFakeTables ()
        let batch = TableBatchOperation()

        createEntityWithString "pk" "1" "rowValue"
        |> TableOperation.Insert
        |> batch.Add

        batch |> table.ExecuteBatch |> ignore

        let batch = TableBatchOperation()
        createEntityWithString "pk" "2" "rowValue"
        |> TableOperation.Insert
        |> batch.Add

        batch |> table.ExecuteBatch |> ignore

        let actual =
          TableOperation.Retrieve<DynamicTableEntity>("pk", "1")
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 200 "unexpected result"

        let result =
          actual.Result |> unbox<DynamicTableEntity>

        Expect.equal (result.PartitionKey) "pk" "unexpected value"
        Expect.equal (result.RowKey) "1" "unexpected value"

        let actual =
          TableOperation.Retrieve<DynamicTableEntity>("pk", "2")
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 200 "unexpected result"

        let result =
          actual.Result |> unbox<DynamicTableEntity>

        Expect.equal (result.PartitionKey) "pk" "unexpected value"
        Expect.equal (result.RowKey) "2" "unexpected value"

      } ]
