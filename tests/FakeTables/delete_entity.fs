module delete_entity

open Expecto
open System
open Host
open Microsoft.Azure.Cosmos.Table


[<Tests>]
let deleteTests =
  testList
    "delete_entity"
    [ test "entity exists" {
        let table = createFakeTables ()

        let entity =
          DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())

        entity
        |> TableOperation.Insert
        |> table.Execute
        |> ignore

        let actual =
          TableOperation.Delete(entity) |> table.Execute

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"

        let actual =
          TableOperation.Retrieve<DynamicTableEntity>("pk2", "r2k")
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 404 "unexpected result"

      }
      test "entity doesnt exist" {
        let table = createFakeTables ()

        let entity =
          DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())

        let actual =
          TableOperation.Delete(entity) |> table.Execute

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"

      } ]