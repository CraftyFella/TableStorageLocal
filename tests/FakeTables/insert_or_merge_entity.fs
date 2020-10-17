module insert_or_merge_entity

open Expecto
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
        Expect.isNotNull (actual.Etag) "eTag is expected"
      }

      test "row exists is accepted" {
        let table = createFakeTables ()

        let fields = allFieldTypes ()

        DynamicTableEntity("pk2", "r2k", "*", fields)
        |> TableOperation.Insert
        |> table.Execute
        |> ignore

        fields.["StringField"] <- EntityProperty.GeneratePropertyForString "updated"

        let actual =
          DynamicTableEntity("pk2", "r2k", "*", fields)
          |> TableOperation.InsertOrMerge
          |> table.Execute

        let result =
          TableOperation.Retrieve<DynamicTableEntity>("pk2", "r2k")
          |> table.Execute
          |> fun r -> r.Result |> unbox<DynamicTableEntity>

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"
        Expect.equal result.["StringField"] (fields.["StringField"]) ""
      }

      test "merge single property" {
        let table = createFakeTables ()

        let fields = allFieldTypes ()

        DynamicTableEntity("pk2", "r2k", "*", fields)
        |> TableOperation.Insert
        |> table.Execute
        |> ignore

        let actual =
          createEntityWithString "pk2" "rk2" "updated"
          |> TableOperation.InsertOrMerge
          |> table.Execute

        let result =
          TableOperation.Retrieve<DynamicTableEntity>("pk2", "r2k")
          |> table.Execute
          |> fun r -> r.Result |> unbox<DynamicTableEntity>

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"
        Expect.equal result.["StringField"] (fields.["StringField"]) "unexpected value"
      }

      test "add single property" {
        let table = createFakeTables ()

        let fields = allFieldTypes ()

        DynamicTableEntity("pk2", "r2k", "*", fields)
        |> TableOperation.Insert
        |> table.Execute
        |> ignore

        let actual =
          fields.Add("NewStringField", EntityProperty.GeneratePropertyForString "new")
          DynamicTableEntity("pk2", "r2k", "*", fields)
          |> TableOperation.InsertOrMerge
          |> table.Execute

        let result =
          TableOperation.Retrieve<DynamicTableEntity>("pk2", "r2k")
          |> table.Execute
          |> fun r -> r.Result |> unbox<DynamicTableEntity>

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"
        Expect.equal result.["NewStringField"] (fields.["NewStringField"]) "unexpected value"
      }

      test "inserted row is retrievable" {
        let table = createFakeTables ()
        let fields = allFieldTypes ()
        let insertedResult =
          DynamicTableEntity("pk2", "r2k", "*", fields)
          |> TableOperation.InsertOrMerge
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
