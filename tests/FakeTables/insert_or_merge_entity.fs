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

      } ]