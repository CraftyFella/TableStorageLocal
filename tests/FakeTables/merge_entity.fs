module merge_entity

open Expecto
open Microsoft.Azure.Cosmos.Table


[<Tests>]
let mergeTests =
  testList
    "merge"
    [ test "row exists and correct etag used is accepted" {
        let table = createFakeTables ()

        let actual =
          DynamicTableEntity("pk2", "r2k", null, stringFieldType "Inserted Value")
          |> TableOperation.Insert
          |> table.Execute

        Expect.isNotNull (actual.Etag) "eTag is expected"

        Async.Sleep 1 |> Async.RunSynchronously

        let actualMerge =
          DynamicTableEntity("pk2", "r2k", actual.Etag, stringFieldType "Updated Value")
          |> TableOperation.Merge
          |> table.Execute

        let actualRetrieve =
          TableOperation.Retrieve<DynamicTableEntity>("pk2", "r2k")
          |> table.Execute

        Expect.isNotNull (actualMerge.Etag) "eTag is expected"
        Expect.isNotNull (actualRetrieve.Etag) "eTag is expected"
        Expect.equal (actualMerge.HttpStatusCode) 204 "unexpected result"
        Expect.equal (actualMerge.Etag) (actualRetrieve.Etag) "unexpected etag"
      }

      test "row exists and old etag used is rejected" {
        let table = createFakeTables ()

        let oldEtag = "W/\"datetime'2020-10-16T10:37:44Z'\""

        let _ =
          DynamicTableEntity("pk2", "r2k", null, stringFieldType "Inserted Value")
          |> TableOperation.Insert
          |> table.Execute

        let run () =
          DynamicTableEntity("pk2", "r2k", oldEtag, stringFieldType "Updated Value")
          |> TableOperation.Merge
          |> table.Execute
          |> ignore

        Expect.throwsTWithPredicate<Microsoft.Azure.Cosmos.Table.StorageException> (fun e ->
          e.RequestInformation.HttpStatusCode = 412) run "expected exception"

      }

      test "row exists and wildcard (*) etag used is accepted" {
        let table = createFakeTables ()

        let wildcardEtag = "*"

        let _ =
          DynamicTableEntity("pk2", "r2k", null, stringFieldType "Inserted Value")
          |> TableOperation.Insert
          |> table.Execute

        let actual =
          DynamicTableEntity("pk2", "r2k", wildcardEtag, stringFieldType "Updated Value")
          |> TableOperation.Merge
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"

      }

      test "merge single property" {
        let table = createFakeTables ()

        let fields = allFieldTypes ()

        let actual =
          DynamicTableEntity("pk2", "r2k", "*", fields)
          |> TableOperation.Insert
          |> table.Execute

        let actual =
          createEntityWithStringAndETag "pk2" "r2k" actual.Etag "updated"
          |> TableOperation.Merge
          |> table.Execute

        let result =
          TableOperation.Retrieve<DynamicTableEntity>("pk2", "r2k")
          |> table.Execute
          |> fun r -> r.Result |> unbox<DynamicTableEntity>

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"
        Expect.equal result.["StringField"] (EntityProperty.GeneratePropertyForString("updated")) "unexpected value"
      }

      test "add single property" {
        let table = createFakeTables ()

        let fields = allFieldTypes ()

        let actual =
          DynamicTableEntity("pk2", "r2k", "*", fields)
          |> TableOperation.Insert
          |> table.Execute

        let actual =
          fields.Add("NewStringField", EntityProperty.GeneratePropertyForString "new")
          DynamicTableEntity("pk2", "r2k", actual.Etag, fields)
          |> TableOperation.Merge
          |> table.Execute

        let result =
          TableOperation.Retrieve<DynamicTableEntity>("pk2", "r2k")
          |> table.Execute
          |> fun r -> r.Result |> unbox<DynamicTableEntity>

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"
        Expect.equal result.["NewStringField"] (fields.["NewStringField"]) "unexpected value"
      }

      test "update single property" {
        let table = createFakeTables ()

        let fields = allFieldTypes ()

        let actual =
          DynamicTableEntity("pk2", "r2k", "*", fields)
          |> TableOperation.Insert
          |> table.Execute

        let actual =
          DynamicTableEntity("pk2", "r2k", actual.Etag, stringFieldType "new")
          |> TableOperation.Merge
          |> table.Execute

        let result =
          TableOperation.Retrieve<DynamicTableEntity>("pk2", "r2k")
          |> table.Execute
          |> fun r -> r.Result |> unbox<DynamicTableEntity>

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"
        Expect.equal result.["StringField"] (EntityProperty.GeneratePropertyForString "new") "unexpected value"
      } ]
