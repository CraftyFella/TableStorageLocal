module add_table

open Expecto
open FakeAzureTables

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

      test "non alpha numeric value" {
        let tables = new FakeTables()

        let table =
          tables.Client.GetTableReference "invalid_name"

        let actual = table.CreateIfNotExists()
        Expect.equal actual false "unexpected result"
      }

      test "starts with numeric" {
        let tables = new FakeTables()

        let table =
          tables.Client.GetTableReference "8c1fe47d43034d17a0c6fcebc6d802e7"

        let actual = table.CreateIfNotExists()
        Expect.equal actual false "unexpected result"
      }

      test "less than 3 alpha" {
        let tables = new FakeTables()

        let table = tables.Client.GetTableReference "ab"

        let actual = table.CreateIfNotExists()
        Expect.equal actual false "unexpected result"
      }

      test "more than than 63 alpha" {
        let tables = new FakeTables()

        let table =
          tables.Client.GetTableReference "a123456789012345678901234567890123456789012345678901234567890123"

        let actual = table.CreateIfNotExists()
        Expect.equal actual false "unexpected result"
      } ]
