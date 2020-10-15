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

      } ]