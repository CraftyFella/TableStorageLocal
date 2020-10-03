module Domain

open System
open System.Collections.Generic

[<RequireQualifiedAccess>]
type QueryComparison =
  | Equal
  | GreaterThan
  | GreaterThanOrEqual
  | LessThan
  | LessThanOrEqual
  | NotEqual

[<RequireQualifiedAccess>]
type FieldValue =
  | String of string
  | Bool of bool
  | Binary of byte array
  | Date of DateTimeOffset
  | Double of float
  | Guid of Guid
  | Int of int32
  | Long of int64
  member __.AsString =
    match __ with
    | String value -> string value
    | Bool value -> string value
    | Binary value -> string value
    | Date value -> string value
    | Double value -> string value
    | Guid value -> string value
    | Int value -> string value
    | Long value -> string value

[<RequireQualifiedAccess>]
type TableOperators =
  | And
  | Or

[<RequireQualifiedAccess>]
type Filter =
  | PartitionKey of QueryComparison * string
  | RowKey of QueryComparison * string
  | Property of name: string * QueryComparison * FieldValue
  | Combined of Filter * TableOperators * Filter

type TableFields = IDictionary<string, FieldValue>

type TableKeys =
  { PartitonKey: string
    RowKey: string }

type TableRow = TableKeys * TableFields

type Tables = IDictionary<string, IDictionary<TableKeys, TableFields>>    // Need this to be a dictionary so I can add things to it

type Command =
  | CreateTable of Name: string
  | Insert of Table: string * TableRow
  | InsertOrMerge of Table: string * TableRow
  | Delete of Table: string * TableKeys
  | Get of Table: string * TableKeys
  | Query of Table: string * Filter: string


type ConflictReason =
  | TableAlreadyExists
  | KeyAlreadyExists

type CommandResult =
  | Ack
  | Conflict of ConflictReason
  | GetResponse of TableRow
  | QueryResponse of TableRow list
  | NotFound


module TableFields =
  open Newtonsoft.Json.Linq

  let toJProperties (tableFields : TableFields) =
    let fields =
      tableFields
      |> Seq.toList
      |> List.map (fun entry ->
           let name = entry.Key
           let value = entry.Value
           match value with
           | FieldValue.String value -> [ JProperty(name, value) ]
           | FieldValue.Long value ->
               [ JProperty(name, string value)
                 JProperty(sprintf "%s@odata.type" name, "Edm.Int64") ]
           | FieldValue.Int value -> [ JProperty(name, value) ]
           | FieldValue.Guid value ->
               [ JProperty(name, value)
                 JProperty(sprintf "%s@odata.type" name, "Edm.Guid") ]
           | FieldValue.Double value -> [ JProperty(name, value) ]
           | FieldValue.Date value ->
               [ JProperty(name, value)
                 JProperty(sprintf "%s@odata.type" name, "Edm.DateTime") ]
           | FieldValue.Bool value -> [ JProperty(name, value) ]
           | FieldValue.Binary value ->
               [ JProperty(name, value)
                 JProperty(sprintf "%s@odata.type" name, "Edm.Binary") ]

           )
      |> List.collect id

    fields

  let fromJObject (jObject: JObject) =
    let oDataFields =
      jObject.Properties()
      |> Seq.filter (fun p -> p.Name.EndsWith "@odata.type")
      |> Seq.map (fun p -> (p.Name.Replace("@odata.type", ""), p.Value.Value<string>()))
      |> Map.ofSeq

    let oDataType name =
      match oDataFields.TryGetValue(name) with
      | true, t -> t
      | false, _ -> "Unknown"

    jObject.Properties()
    |> Seq.filter (fun p -> p.Name <> "PartitionKey")
    |> Seq.filter (fun p -> p.Name <> "RowKey")
    |> Seq.filter (fun p -> p.Name.Contains "odata.type" |> not)
    |> Seq.map (fun p ->
           (p.Name,
            match p.Name |> oDataType with
             | "Edm.DateTime" ->
                 p.Value.Value<DateTime>()
                 |> DateTimeOffset
                 |> FieldValue.Date
             | "Edm.Int64" -> p.Value.Value<int64>() |> FieldValue.Long
             | "Edm.Guid" ->
                 p.Value.Value<string>()
                 |> Guid.Parse
                 |> FieldValue.Guid
             | "Edm.Binary" ->
                 p.Value.Value<string>()
                 |> Convert.FromBase64String
                 |> FieldValue.Binary
             | _ ->
                 match p.Value.Type with
                 | JTokenType.Integer -> p.Value.Value<int>() |> FieldValue.Int
                 | JTokenType.Float -> p.Value.Value<float>() |> FieldValue.Double
                 | JTokenType.Boolean -> p.Value.Value<bool>() |> FieldValue.Bool
                 | _ -> p.Value.Value<string>() |> FieldValue.String
                 ))
    |> dict

module TableRow =
  open Newtonsoft.Json.Linq

  let toJObject (tablekeys : TableKeys, tableFields : TableFields) =
    let requiredFields =
      [ JProperty("PartitionKey", tablekeys.PartitonKey)
        JProperty("RowKey", tablekeys.RowKey) ]

    let fields = TableFields.toJProperties tableFields

    JObject(requiredFields |> List.append fields)