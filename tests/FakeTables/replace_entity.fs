module replace_entity

open Expecto
open System
open FakeAzureTables.Host
open Microsoft.Azure.Cosmos.Table


[<Tests>]
let replaceTests =
  testList
    "replace"
    [ test "row exists and correct etag used is accepted" {
        let table = createFakeTables ()

        let actual =
          DynamicTableEntity("pk2", "r2k", null, stringFieldType "Inserted Value")
          |> TableOperation.Insert
          |> table.Execute

        Expect.isNotNull (actual.Etag) "eTag is expected"

        let _ =
          TableOperation.Retrieve<DynamicTableEntity>("pk2", "r2k")
          |> table.Execute

        let actual =
          DynamicTableEntity("pk2", "r2k", actual.Etag, stringFieldType "Updated Value")
          |> TableOperation.Replace
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"
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
          |> TableOperation.Replace
          |> table.Execute
          |> ignore

        Expect.throwsTWithPredicate<Microsoft.Azure.Cosmos.Table.StorageException> (fun e ->
          e.Message = "Precondition Failed") run "expected exception"

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
          |> TableOperation.Replace
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"

      } ]
