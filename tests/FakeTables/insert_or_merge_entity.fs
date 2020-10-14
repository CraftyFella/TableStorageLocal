module insert_or_merge_entity

open Expecto
open System
open Host
open Microsoft.Azure.Cosmos.Table

[<Tests>]
let insertOrMergeTests =
  testList
    "insertOrMerge"
    [ test "row doesn't exist is accepted" {
        let table = createFakeTables ()

        let actual =
          DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())
          |> TableOperation.InsertOrMerge
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"
      }
      test "row exists is accepted" {
        let table = createFakeTables ()

        DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())
        |> TableOperation.Insert
        |> table.Execute
        |> ignore


        let actual =
          DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())
          |> TableOperation.InsertOrMerge
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"

      }
      test "inserted row is retrievable" {
        let table = createFakeTables ()
        let fields = allFieldTypes ()
        DynamicTableEntity("pk2", "r2k", "*", fields)
        |> TableOperation.InsertOrMerge
        |> table.Execute
        |> ignore

        let actual =
          TableOperation.Retrieve<DynamicTableEntity>("pk2", "r2k")
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 200 "unexpected result"

        let result =
          actual.Result |> unbox<DynamicTableEntity>

        Expect.equal (result.PartitionKey) "pk2" "unexpected value"
        Expect.equal (result.RowKey) "r2k" "unexpected value"

        for field in fields do
          Expect.equal (result.Properties.[field.Key]) (field.Value) "unexpected values"
      } ]
