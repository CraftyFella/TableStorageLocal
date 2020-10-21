module search_entities

open Expecto
open Microsoft.Azure.Cosmos.Table
open System

[<Tests>]
let searchTests =
  testList
    "search_entities"
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
          |> Seq.sort
          |> Seq.toList

        Expect.equal rowKeys [ "rk1"; "rk2"; "rk4"; "rk5" ] "unexpected row Keys"

        for result in results do
          Expect.isNotNull result.ETag "eTag is expected"
          for field in allFieldTypes () do
            Expect.equal (result.Properties.[field.Key]) (field.Value) "unexpected values"

      }

      test "all rows" {
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

        let query = TableQuery<DynamicTableEntity>()

        let token = TableContinuationToken()

        let results =
          table.ExecuteQuerySegmented(query, token)

        let rowKeys =
          results
          |> Seq.map (fun r -> r.RowKey)
          |> Seq.sort
          |> Seq.toList

        Expect.equal rowKeys [ "rk1"; "rk2"; "rk3"; "rk4"; "rk5" ] "unexpected row Keys"

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

        Expect.equal partitionKeysAndRowKeys [ "pk3", "rk3" ] "unexpected rows"

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
          createEntityWithString "pk5" "rk5" "Three" ]
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
          |> Seq.sort
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
          createEntityWithInt "pk5" "rk5" 3 ]
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
          |> Seq.sort
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

      } 
      
      test "top 1 with matching partition key" {
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
          TableQuery<DynamicTableEntity>().Where(filter).Take(Nullable 1)

        let token = TableContinuationToken()
        
        let results =
          table.ExecuteQuerySegmented(query, token)

        let rowKeys =
          results
          |> Seq.map (fun r -> r.RowKey)
          |> Seq.sort
          |> Seq.toList

        Expect.equal rowKeys [ "rk1" ] "unexpected row Keys"

        for result in results do
          Expect.isNotNull result.ETag "eTag is expected"
          for field in allFieldTypes () do
            Expect.equal (result.Properties.[field.Key]) (field.Value) "unexpected values"

      }]
