module batch

open Expecto
open Microsoft.Azure.Cosmos.Table

[<Tests>]
let batchTests =
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

        let actual =
          batch |> table.ExecuteBatch

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

        let actual =
          batch |> table.ExecuteBatch

        Expect.equal (actual |> Seq.length) 1 "unexpected result"
        for batchItem in actual do
          Expect.equal (batchItem.HttpStatusCode) 204 "unexpected result"

        let batch = TableBatchOperation()
        createEntityWithString "pk" "2" "rowValue"
          |> TableOperation.Insert
          |> batch.Add
        let actual =
          batch |> table.ExecuteBatch

        Expect.equal (actual |> Seq.length) 1 "unexpected result"
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