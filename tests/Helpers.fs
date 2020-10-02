[<AutoOpen>]
module Helpers

open FakeTableStorage
open Microsoft.Azure.Cosmos.Table
open System

let createFakeTables () =
  let tables = new FakeTables()
  let table = tables.Client.GetTableReference "test"
  table.CreateIfNotExists() |> ignore
  table

let allFieldTypes () =
  [ ("StringField", EntityProperty.GeneratePropertyForString("StringValue2"))
    ("DateField", EntityProperty.GeneratePropertyForDateTimeOffset(Nullable DateTimeOffset.UtcNow))
    ("IntField", EntityProperty.GeneratePropertyForInt(Nullable 2))
    ("LongField", EntityProperty.GeneratePropertyForLong(Nullable 2L))
    ("GuidField", EntityProperty.GeneratePropertyForGuid(Nullable(Guid.NewGuid())))
    ("FloatField", EntityProperty.GeneratePropertyForDouble(Nullable 2.))
    ("BoolField", EntityProperty.GeneratePropertyForBool(Nullable false))
    ("ByteArrayField", EntityProperty.GeneratePropertyForByteArray([| 104uy; 101uy; 108uy; 108uy; 111uy |])) ]
  |> dict
