open System
open Microsoft.Azure.Cosmos.Table
open TableStorageLocal

[<EntryPoint>]
let main argv =
  use tables = new LocalTables()
  let client = CloudStorageAccount.Parse(tables.ConnectionString).CreateCloudTableClient()
  let table = client.GetTableReference "test"
  table.CreateIfNotExists() |> ignore
  let fields =
    [ ("StringField", EntityProperty.GeneratePropertyForString("StringValue1"))
      ("DateField", EntityProperty.GeneratePropertyForDateTimeOffset(Nullable DateTimeOffset.UtcNow))
      ("IntField", EntityProperty.GeneratePropertyForInt(Nullable 1))
      ("LongField", EntityProperty.GeneratePropertyForLong(Nullable 1L))
      ("GuidField", EntityProperty.GeneratePropertyForGuid(Nullable(Guid.NewGuid())))
      ("FloatField", EntityProperty.GeneratePropertyForDouble(Nullable 1.))
      ("BoolField", EntityProperty.GeneratePropertyForBool(Nullable true))
      ("ByteArrayField", EntityProperty.GeneratePropertyForByteArray([| 104uy; 101uy; 108uy; 108uy; 111uy |]))
      ]
    |> dict

  DynamicTableEntity("pk", "rk", "*", fields)
  |> TableOperation.InsertOrReplace
  |> table.Execute
  |> ignore

  let fields2 =
    [ ("StringField", EntityProperty.GeneratePropertyForString("StringValue2"))
      ("DateField", EntityProperty.GeneratePropertyForDateTimeOffset(Nullable DateTimeOffset.UtcNow))
      ("IntField", EntityProperty.GeneratePropertyForInt(Nullable 2))
      ("LongField", EntityProperty.GeneratePropertyForLong(Nullable 2L))
      ("GuidField", EntityProperty.GeneratePropertyForGuid(Nullable(Guid.NewGuid())))
      ("FloatField", EntityProperty.GeneratePropertyForDouble(Nullable 2.))
      ("BoolField", EntityProperty.GeneratePropertyForBool(Nullable false))
      ("ByteArrayField", EntityProperty.GeneratePropertyForByteArray([| 104uy; 101uy; 108uy; 108uy; 111uy |])) ]
    |> dict

  DynamicTableEntity("pk2", "r2k", "*", fields2)
  |> TableOperation.InsertOrReplace
  |> table.Execute
  |> ignore

  let left = TableQuery.GenerateFilterConditionForDouble("FloatField", QueryComparisons.Equal, 2.)
  let right = TableQuery.GenerateFilterConditionForLong("LongField", QueryComparisons.GreaterThanOrEqual, 2L)
  let filter = TableQuery.CombineFilters(left, Microsoft.Azure.Cosmos.Table.TableOperators.Or, right)
  let query = TableQuery<DynamicTableEntity>().Where filter
  let token = TableContinuationToken()
  let results = table.ExecuteQuerySegmentedAsync(query, token).Result
  for result in results do
    printfn "PartitionKey is %A" result.PartitionKey
    printfn "RowKey is %A" result.RowKey
    printfn "Properties are %A" (result.Properties |> List.ofSeq)

  printfn "ConnectionString is %A" tables.ConnectionString
  Console.ReadLine() |> ignore

  0
