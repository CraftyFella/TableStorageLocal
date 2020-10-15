[<AutoOpen>]
module Helpers

open Host
open Microsoft.Azure.Cosmos.Table
open System
open System.Collections.Generic

type FakeTables with
  member __.Client =
    CloudStorageAccount.Parse(__.ConnectionString).CreateCloudTableClient()

let createFakeTables () =
  let tables = new FakeTables()
  let table = tables.Client.GetTableReference "test"
  table.CreateIfNotExists() |> ignore
  table

let allFieldTypes () =
  let now =
    DateTimeOffset(2000, 1, 1, 1, 1, 1, 1, TimeSpan.Zero)

  [ ("StringField", EntityProperty.GeneratePropertyForString("StringValue"))
    ("DateField", EntityProperty.GeneratePropertyForDateTimeOffset(Nullable now))
    ("IntField", EntityProperty.GeneratePropertyForInt(Nullable 2))
    ("LongField", EntityProperty.GeneratePropertyForLong(Nullable 3L))
    ("GuidField", EntityProperty.GeneratePropertyForGuid(Nullable(Guid.Empty)))
    ("FloatField", EntityProperty.GeneratePropertyForDouble(Nullable 4.))
    ("BoolField", EntityProperty.GeneratePropertyForBool(Nullable true))
    ("ByteArrayField", EntityProperty.GeneratePropertyForByteArray([| 104uy; 101uy; 108uy; 108uy; 111uy |])) ]
  |> dict
  |> Dictionary

let createEntity pk rk =
  DynamicTableEntity(pk, rk, "*", allFieldTypes ())

let createEntityWithString pk rk stringValue =
  DynamicTableEntity
    (pk,
     rk,
     "*",
     [ ("StringField", EntityProperty.GeneratePropertyForString(stringValue)) ]
     |> dict)

let createEntityWithInt pk rk intValue =
  DynamicTableEntity
    (pk,
     rk,
     "*",
     [ ("IntField", EntityProperty.GeneratePropertyForInt(Nullable intValue)) ]
     |> dict)
