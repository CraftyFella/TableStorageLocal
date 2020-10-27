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

      test "multiple changes with same row key" {

        let table = createFakeTables ()
        let batch = TableBatchOperation()

        createEntityWithString "pk" "1" "rowValue"
        |> TableOperation.Insert
        |> batch.Add

        createEntityWithString "pk" "1" "rowValue"
        |> TableOperation.Insert
        |> batch.Add

        let run() = batch |> table.ExecuteBatch |> ignore

        Expect.throwsTWithPredicate<Microsoft.Azure.Cosmos.Table.StorageException> (fun e -> e.RequestInformation.HttpStatusCode = 400) run "expected exception"

      }

      test "batch tries to insert a row which already exists" {

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


        let run() = batch |> table.ExecuteBatch |> ignore

        Expect.throwsTWithPredicate<Microsoft.Azure.Cosmos.Table.StorageException> (fun e -> e.RequestInformation.HttpStatusCode = 409) run "expected exception"

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
