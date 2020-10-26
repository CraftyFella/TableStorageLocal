module list_tables

open Expecto
open FakeAzureTables.Host

[<Tests>]
let addTableTests =
  testList
    "list_tables"
    [ test "table returned" {
        let tables = new FakeTables()
        let table = tables.Client.GetTableReference "test"
        table.CreateIfNotExists() |> ignore
        let actual = tables.Client.ListTables() |> Seq.head
        Expect.equal actual.Uri table.Uri "unexpected result"
        Expect.equal actual.Name table.Name "unexpected result"
      } ]
