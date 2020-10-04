module FakeTablesTests

open Expecto
open System
open Host
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
          TableOperation.Delete(entity) |> table.Execute

        Expect.equal (actual.HttpStatusCode) 204 "unexpected result"

      } ]

[<Tests>]
let searchTests =
  testList
    "search"
    [ test "all with matching partition key" {
        let table = createFakeTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntity "pk1" "rk1"
          createEntity "pk1" "rk2"
          createEntity "pk2" "rk3"
          createEntity "pk1" "rk4"
          createEntity "pk1" "rk5" ]
        |> List.iter insert

        let filter =
          TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "pk1")

        let query =
          TableQuery<DynamicTableEntity>().Where filter

        let token = TableContinuationToken()

        let results =
          table.ExecuteQuerySegmented(query, token)

        let rowKeys =
          results
          |> Seq.map (fun r -> r.RowKey)
          |> Seq.toList

        Expect.equal rowKeys [ "rk1"; "rk2"; "rk4"; "rk5" ] "unexpected row Keys"

        for result in results do
          for field in allFieldTypes () do
            Expect.equal (result.Properties.[field.Key]) (field.Value) "unexpected values"

      }

      test "all with matching row key" {
        let table = createFakeTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntity "pk1" "rk1"
          createEntity "pk2" "rk2"
          createEntity "pk3" "rk1"
          createEntity "pk4" "rk4"
          createEntity "pk5" "rk5" ]
        |> List.iter insert

        let filter =
          TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "rk1")

        let query =
          TableQuery<DynamicTableEntity>().Where filter

        let token = TableContinuationToken()

        let results =
          table.ExecuteQuerySegmented(query, token)

        let partitionKeys =
          results
          |> Seq.map (fun r -> r.PartitionKey)
          |> Seq.toList

        Expect.equal partitionKeys [ "pk1"; "pk3" ] "unexpected partition Keys"

        for result in results do
          for field in allFieldTypes () do
            Expect.equal (result.Properties.[field.Key]) (field.Value) "unexpected values"

      }

      test "partionKey OR rowKey" {
        let table = createFakeTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntity "pk1" "rk1"
          createEntity "pk2" "rk2"
          createEntity "pk3" "rk3"
          createEntity "pk4" "rk4"
          createEntity "pk5" "rk5" ]
        |> List.iter insert

        let partitionKeyFilter =
          TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "pk3")

        let rowKeyFilter =
          TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "rk5")

        let filter =
          TableQuery.CombineFilters(partitionKeyFilter, TableOperators.Or, rowKeyFilter)

        let query =
          TableQuery<DynamicTableEntity>().Where filter

        let token = TableContinuationToken()

        let results =
          table.ExecuteQuerySegmented(query, token)

        let partitionKeysAndRowKeys =
          results
          |> Seq.map (fun r -> r.PartitionKey, r.RowKey)
          |> Seq.toList

        Expect.equal partitionKeysAndRowKeys [ "pk3", "rk3"; "pk5", "rk5" ] "unexpected rows"

        for result in results do
          for field in allFieldTypes () do
            Expect.equal (result.Properties.[field.Key]) (field.Value) "unexpected values"

      }

      test "partionKey AND rowKey" {
        let table = createFakeTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntity "pk1" "rk1"
          createEntity "pk2" "rk3"
          createEntity "pk3" "rk3"
          createEntity "pk3" "rk4"
          createEntity "pk5" "rk5" ]
        |> List.iter insert

        let partitionKeyFilter =
          TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "pk3")

        let rowKeyFilter =
          TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "rk3")

        let filter =
          TableQuery.CombineFilters(partitionKeyFilter, TableOperators.And, rowKeyFilter)

        let query =
          TableQuery<DynamicTableEntity>().Where filter

        let token = TableContinuationToken()

        let results =
          table.ExecuteQuerySegmented(query, token)

        let partitionKeysAndRowKeys =
          results
          |> Seq.map (fun r -> r.PartitionKey, r.RowKey)
          |> Seq.toList

        Expect.equal partitionKeysAndRowKeys [ "pk3", "rk3"; ] "unexpected rows"

        for result in results do
          for field in allFieldTypes () do
            Expect.equal (result.Properties.[field.Key]) (field.Value) "unexpected values"

      }

      test "property String search" {
        let table = createFakeTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntityWithString "pk1" "rk1" "One"
          createEntityWithString "pk2" "rk3" "Two"
          createEntityWithString "pk3" "rk3" "One"
          createEntityWithString "pk3" "rk4" "Two"
          createEntityWithString "pk5" "rk5" "Three"]
        |> List.iter insert

        let filter =
          TableQuery.GenerateFilterCondition("StringField", QueryComparisons.Equal, "Two")

        let query =
          TableQuery<DynamicTableEntity>().Where filter

        let token = TableContinuationToken()

        let results =
          table.ExecuteQuerySegmented(query, token)

        let partitionKeysAndRowKeys =
          results
          |> Seq.map (fun r -> r.PartitionKey, r.RowKey)
          |> Seq.toList

        Expect.equal partitionKeysAndRowKeys [ "pk2", "rk3"; "pk3", "rk4" ] "unexpected rows"

        for result in results do
          Expect.equal (result.Properties.["StringField"].StringValue) "Two" "unexpected values"

      }

      test "property Int search" {
        let table = createFakeTables ()

        let insert entity =
          entity
          |> TableOperation.Insert
          |> table.Execute
          |> ignore

        [ createEntityWithInt "pk1" "rk1" 1
          createEntityWithInt "pk2" "rk3" 2
          createEntityWithInt "pk3" "rk3" 1
          createEntityWithInt "pk3" "rk4" 2
          createEntityWithInt "pk5" "rk5" 3]
        |> List.iter insert

        let filter =
          TableQuery.GenerateFilterConditionForInt("IntField", QueryComparisons.Equal, 2)

        let query =
          TableQuery<DynamicTableEntity>().Where filter

        let token = TableContinuationToken()

        let results =
          table.ExecuteQuerySegmented(query, token)

        let partitionKeysAndRowKeys =
          results
          |> Seq.map (fun r -> r.PartitionKey, r.RowKey)
          |> Seq.toList

        Expect.equal partitionKeysAndRowKeys [ "pk2", "rk3"; "pk3", "rk4" ] "unexpected rows"

        for result in results do
          Expect.equal (result.Properties.["IntField"].Int32Value.Value) 2 "unexpected values"

      }

      test "no matchses" {
        let table = createFakeTables ()

        let filter =
          TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "pk1")

        let query =
          TableQuery<DynamicTableEntity>().Where filter

        let token = TableContinuationToken()

        let results =
          table.ExecuteQuerySegmented(query, token)

        Expect.equal (results |> Seq.length) 0 "unexpected length"

      } ]
