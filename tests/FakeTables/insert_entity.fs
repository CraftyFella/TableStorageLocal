module insert_entity

open Expecto
open Microsoft.Azure.Cosmos.Table

[<Tests>]
let insertTests =
  testList
    "Insert"
    [ test "row doesn't exist is accepted" {
        let table = createLocalTables ()

        let actual =
          DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())
          |> TableOperation.Insert
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"
        Expect.isNotNull (actual.Etag) "eTag is expected"
      }

      test "row exists causes conflict exception" {
        let table = createLocalTables ()

        DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())
        |> TableOperation.Insert
        |> table.Execute
        |> ignore


        let run () =
          DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        Expect.throwsTWithPredicate<Microsoft.Azure.Cosmos.Table.StorageException>
          (fun e -> e.RequestInformation.HttpStatusCode = 409)
          run
          "expected exception"
      }

      test "row exists (Different case) is accepted" {
        let table = createLocalTables ()

        DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())
        |> TableOperation.Insert
        |> table.Execute
        |> ignore

        let actual =
          DynamicTableEntity("PK2", "R2K", "*", allFieldTypes ())
          |> TableOperation.Insert
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"
        Expect.isNotNull (actual.Etag) "eTag is expected"
      }

      test "inserted row is retrievable" {
        let table = createLocalTables ()
        let fields = allFieldTypes ()

        let insertedResult =
          DynamicTableEntity("pk2", "r2k", "*", fields)
          |> TableOperation.Insert
          |> table.Execute

        let actual =
          TableOperation.Retrieve<DynamicTableEntity>("pk2", "r2k")
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 200 "unexpected result"

        let result =
          actual.Result |> unbox<DynamicTableEntity>

        Expect.equal (result.PartitionKey) "pk2" "unexpected value"
        Expect.equal (result.RowKey) "r2k" "unexpected value"
        Expect.isNotNull (actual.Etag) "eTag is expected"
        Expect.equal insertedResult.Etag (actual.Etag) "eTags should match"

        for field in fields do
          Expect.equal (result.Properties.[field.Key]) (field.Value) "unexpected values"
      } ]
