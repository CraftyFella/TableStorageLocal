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

[<Tests>]
let insertOrMergeTests =
  testList
    "insertOrMerge"
    [ test "row doesn't exist is accepted" {
        let table = createFakeTables ()

        let actual =
          DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())
          |> TableOperation.InsertOrMerge
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"
      }
      test "row exists is accepted" {
        let table = createFakeTables ()

        DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())
        |> TableOperation.Insert
        |> table.Execute
        |> ignore


        let actual =
          DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())
          |> TableOperation.InsertOrMerge
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"

      } ]

[<Tests>]
let insertOrReplaceTests =
  testList
    "insertOrReplace"
    [ test "row doesn't exist is accepted" {
        let table = createFakeTables ()

        let actual =
          DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())
          |> TableOperation.InsertOrReplace
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"
      }
      test "row exists is accepted" {
        let table = createFakeTables ()

        DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())
        |> TableOperation.Insert
        |> table.Execute
        |> ignore


        let actual =
          DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())
          |> TableOperation.InsertOrReplace
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"

      } ]

[<Tests>]
let getTests =
  testList
    "get"
    [ test "entity exists" {
        let table = createFakeTables ()

        DynamicTableEntity("pk2", "r2k", "*", allFieldTypes ())
        |> TableOperation.InsertOrReplace
        |> table.Execute
        |> ignore

        let actual =
          TableOperation.Retrieve<DynamicTableEntity>("pk2", "r2k")
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 200 "unexpected result"

        let result =
          actual.Result |> unbox<DynamicTableEntity>

        Expect.equal (result.PartitionKey) "pk2" "unexpected value"
        Expect.equal (result.RowKey) "r2k" "unexpected value"

        for field in allFieldTypes () do
          Expect.equal (result.Properties.[field.Key]) (field.Value) "unexpected values"

      }
      test "entity doesnt exist" {
        let table = createFakeTables ()

        let actual =
          TableOperation.Retrieve<DynamicTableEntity>("pk2", "r2k")
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 404 "unexpected result"

      } ]

[<Tests>]
let deleteTests =
  testList
    "delete"
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
          TableOperation.Delete(entity)
          |> table.Execute

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"

      } ]
