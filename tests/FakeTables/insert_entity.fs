module insert_entity

open Expecto
open System
open Host
open Microsoft.Azure.Cosmos.Table


[<Tests>]
let insertTests =
  testList
    "Insert"
    [ test "row doesn't exist is accepted" {
        let table = createFakeTables ()

        let actual =
          DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())
          |> TableOperation.Insert
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"
      }

      test "row exists causes conflict exception" {
        let table = createFakeTables ()

        DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())
        |> TableOperation.Insert
        |> table.Execute
        |> ignore


        let run () =
          DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        Expect.throwsT<Microsoft.Azure.Cosmos.Table.StorageException> run "expected exception"
      } ]