module replace_entity

open Expecto
open System
open Host
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

        Expect.notEqual (actual.Etag) null "etag should be returned"

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

        Expect.throwsT<Microsoft.Azure.Cosmos.Table.StorageException> run "expected exception"

      } ]
