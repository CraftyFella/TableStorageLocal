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

type TableField =
  { Name: string
    Value: FieldValue }

type TableFields = TableField list

type TableRow =
  { PartitonKey: string
    RowKey: string
    Fields: TableFields }   // SHould be a dictionary to stop duplicate fields.

type Tables = Dictionary<string, ResizeArray<TableRow>>

type Command =
  | CreateTable of Name: string
  | Insert of Table: string * TableRow
  | InsertOrMerge of Table: string * TableRow
  | Delete of Table: string * PartitionKey: string * RowKey: string
  | Get of Table: string * PartitionKey: string * RowKey: string
  | Query of Table: string * Filter: string

type CommandResult =
  | Ack
  | GetResponse of TableRow
  | QueryResponse of TableRow list
  | NotFound


module TableFields =
  open Newtonsoft.Json.Linq

  let toJProperties tableFields =
    let fields =
      tableFields
      |> List.map (fun f ->

           match f.Value with
           | FieldValue.String value -> [ JProperty(f.Name, value) ]
           | FieldValue.Long value ->
               [ JProperty(f.Name, string value)
                 JProperty(sprintf "%s@odata.type" f.Name, "Edm.Int64") ]
           | FieldValue.Int value -> [ JProperty(f.Name, value) ]
           | FieldValue.Guid value ->
               [ JProperty(f.Name, value)
                 JProperty(sprintf "%s@odata.type" f.Name, "Edm.Guid") ]
           | FieldValue.Double value -> [ JProperty(f.Name, value) ]
           | FieldValue.Date value ->
               [ JProperty(f.Name, value)
                 JProperty(sprintf "%s@odata.type" f.Name, "Edm.DateTime") ]
           | FieldValue.Bool value -> [ JProperty(f.Name, value) ]
           | FieldValue.Binary value ->
               [ JProperty(f.Name, value)
                 JProperty(sprintf "%s@odata.type" f.Name, "Edm.Binary") ]

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
         { Name = p.Name
           Value =
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
                 | _ -> p.Value.Value<string>() |> FieldValue.String })
    |> Seq.toList

module TableRow =
  open Newtonsoft.Json.Linq

  let toJObject tableRow =
    let requiredFields =
      [ JProperty("PartitionKey", tableRow.PartitonKey)
        JProperty("RowKey", tableRow.RowKey) ]

    let fields = TableFields.toJProperties tableRow.Fields

    JObject(requiredFields |> List.append fields)