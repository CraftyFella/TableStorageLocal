module FakeTablesTests

open Expecto
open System
open FakeTableStorage

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
