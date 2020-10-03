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
  [ ("StringField", EntityProperty.GeneratePropertyForString("StringValue"))
    ("DateField", EntityProperty.GeneratePropertyForDateTimeOffset(Nullable DateTimeOffset.MinValue))
    ("IntField", EntityProperty.GeneratePropertyForInt(Nullable 2))
    ("LongField", EntityProperty.GeneratePropertyForLong(Nullable 3L))
    ("GuidField", EntityProperty.GeneratePropertyForGuid(Nullable(Guid.Empty)))
    ("FloatField", EntityProperty.GeneratePropertyForDouble(Nullable 4.))
    ("BoolField", EntityProperty.GeneratePropertyForBool(Nullable true))
    ("ByteArrayField", EntityProperty.GeneratePropertyForByteArray([| 104uy; 101uy; 108uy; 108uy; 111uy |])) ]
  |> dict
