module FakeTablesTests

open Expecto
open System
open FakeTableStorage
open Microsoft.Azure.Cosmos.Table

[<Tests>]
let addTableTests =
  testList
    "AddTable"
    [ test "table doesnt exist" {
        let tables = new FakeTables()
        let table = tables.Client.GetTableReference "test"
        let actual = table.CreateIfNotExists()

        Expect.equal actual true "unexpected result"

      }

      test "table already exists" {
        let tables = new FakeTables()
        let table = tables.Client.GetTableReference "test"
        let _ = table.CreateIfNotExists()
        let actual = table.CreateIfNotExists()

        Expect.equal actual false "unexpected result"

      } ]

[<Tests>]
let insertTests =
  testList
    "Insert"
    [ test "row doesn't exist is accepted" {
        let table = createFakeTables()

        let actual =
          DynamicTableEntity("pk2", "r2k", "*", allFieldTypes())
          |> TableOperation.Insert
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"
      }

      test "row exists causes conflict" {
        let table = createFakeTables()

        DynamicTableEntity("pk2", "r2k", "*", allFieldTypes())
        |> TableOperation.Insert
        |> table.Execute
        |> ignore

        let actual =
          DynamicTableEntity("pk2", "r2k", "*", allFieldTypes())
          |> TableOperation.Insert
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 409 "unexpected result"
      } ]
