module add_table

open Expecto
open FakeAzureTables.Host

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
      }

      test "invalid table name" {
        let tables = new FakeTables()

        let table =
          tables.Client.GetTableReference "invalid_name"

        let actual = table.CreateIfNotExists()
        Expect.equal actual false "unexpected result"
      }

      test "invalid dblite collection name" {
        let tables = new FakeTables()

        let table =
          tables.Client.GetTableReference "8c1fe47d43034d17a0c6fcebc6d802e7"

        let actual = table.CreateIfNotExists()
        Expect.equal actual true "unexpected result"
      } ]
