namespace FakeAzureTables

module Bson =

  open Domain
  open LiteDB

  [<RequireQualifiedAccess>]
  module TableKeys =
    let toBsonExpression keys =
      Query.EQ("$.Keys.PartitionKey + $.Keys.RowKey", BsonValue(keys.PartitionKey + keys.RowKey))

  [<RequireQualifiedAccess>]
  module Continuation =
    let toBsonExpression (continuation: Continuation) =
      Query.GTE
        ("$.Keys.PartitionKey + $.Keys.RowKey",
         BsonValue
           (continuation.NextPartitionKey
            + continuation.NextRowKey))

  [<RequireQualifiedAccess>]
  module FieldValue =
    open System

    let toBsonValue fieldValue =
      match fieldValue with
      | FieldValue.String v -> BsonValue v
      | FieldValue.Bool v -> BsonValue v
      | FieldValue.Date v -> BsonValue v.UtcDateTime
      | FieldValue.Double v -> BsonValue v
      | FieldValue.Guid v -> BsonValue v
      | FieldValue.Int v -> BsonValue v
      | FieldValue.Long v -> BsonValue v
      | FieldValue.Binary v -> BsonValue v

    let fromBsonValue (bsonValue: BsonValue) =
      if bsonValue.IsBoolean then FieldValue.Bool bsonValue.AsBoolean
      elif bsonValue.IsDateTime then FieldValue.Date(DateTimeOffset(bsonValue.AsDateTime.ToUniversalTime()))
      elif bsonValue.IsDouble then FieldValue.Double bsonValue.AsDouble
      elif bsonValue.IsGuid then FieldValue.Guid bsonValue.AsGuid
      elif bsonValue.IsInt32 then FieldValue.Int bsonValue.AsInt32
      elif bsonValue.IsInt64 then FieldValue.Long bsonValue.AsInt64
      elif bsonValue.IsBinary then FieldValue.Binary bsonValue.AsBinary
      else FieldValue.String bsonValue.AsString

    let mapper () =
      let mapper = new BsonMapper()
      mapper.RegisterType<FieldValue>
        ((fun fieldValue -> toBsonValue fieldValue), (fun bsonValue -> fromBsonValue bsonValue))
      mapper
