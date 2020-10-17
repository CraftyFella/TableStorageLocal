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

[<RequireQualifiedAccess>]
type TableOperators =
  | And
  | Or

[<RequireQualifiedAccess>]
type Filter =
  | All
  | PartitionKey of QueryComparison * string
  | RowKey of QueryComparison * string
  | Property of name: string * QueryComparison * FieldValue
  | Combined of Filter * TableOperators * Filter

type TableFields = Dictionary<string, FieldValue>

[<CLIMutable>]
type TableKeys =
  { PartitionKey: string
    RowKey: string }

[<CLIMutable>]
type TableRow =
  { Keys: TableKeys
    Fields: TableFields }
  member __.Id =
    (__.Keys.PartitionKey + __.Keys.RowKey).ToLower()

  member __.ETag =
    match __.Fields.TryGetValue "Timestamp" with
    | true, FieldValue.Date etag -> etag
    | _ -> Unchecked.defaultof<DateTimeOffset>

module ETag =
  let fromDateTimeOffset (input: DateTimeOffset) =
    sprintf "W/\"datetime'%s'\"" (input.ToString("s") + "Z")

  let toDateTimeOffset (input: string) =
    let pattern = "datetime'(.+)'"

    let result =
      System.Text.RegularExpressions.Regex.Match(input, pattern)

    match result.Success with
    | true ->
        match DateTimeOffset.TryParse result.Groups.[1].Value with
        | true, etag -> etag
        | _ -> failwithf "Not a valid datetimeoffset"
    | _ -> failwithf "Not a valid eTag"


type TableCommand = CreateTable of Table: string

type WriteCommand =
  | Insert of Table: string * TableRow
  | Replace of Table: string * ETag: DateTimeOffset * TableRow
  | InsertOrReplace of Table: string * TableRow
  | InsertOrMerge of Table: string * TableRow
  | Delete of Table: string * TableKeys

type ReadCommand =
  | Get of Table: string * TableKeys
  | Query of Table: string * Filter: string option

type BatchCommand = { Commands: WriteCommand list }

type Command =
  | Write of WriteCommand
  | Read of ReadCommand
  | Table of TableCommand
  | Batch of BatchCommand

type TableConflictReason =
  | TableAlreadyExists
  | InvalidTableName

type WriteConflictReason =
  | KeyAlreadyExists
  | EntityDoesntExist
  | UpdateConditionNotSatisfied

type TableCommandResponse =
  | Ack
  | Conflict of TableConflictReason

type WriteCommandResponse =
  | Ack of TableKeys * ETag: DateTimeOffset
  | Conflict of WriteConflictReason

type ReadCommandResponse =
  | GetResponse of TableRow
  | QueryResponse of TableRow list
  | NotFoundResponse

type BatchCommandResponse =
  { CommandResponses: WriteCommandResponse list }

type CommandResult =
  | TableResponse of TableCommandResponse
  | WriteResponse of WriteCommandResponse
  | ReadResponse of ReadCommandResponse
  | BatchResponse of BatchCommandResponse
  | NotFoundResponse

module TableFields =
  open Newtonsoft.Json.Linq

  let toJProperties (tableFields: TableFields) =
    let fields =
      tableFields
      |> Seq.toList
      |> List.map (fun entry ->
           let name = entry.Key
           let value = entry.Value
           match value with
           | FieldValue.String value -> [ JProperty(name, value) ]
           | FieldValue.Long value ->
               [ JProperty(sprintf "%s@odata.type" name, "Edm.Int64")
                 JProperty(name, string value) ]
           | FieldValue.Int value -> [ JProperty(name, value) ]
           | FieldValue.Guid value ->
               [ JProperty(sprintf "%s@odata.type" name, "Edm.Guid")
                 JProperty(name, value) ]
           | FieldValue.Double value -> [ JProperty(name, value) ]
           | FieldValue.Date value when name = "Timestamp" ->
               [ JProperty("odata.etag", value |> ETag.fromDateTimeOffset)
                 JProperty(name, value) ]
           | FieldValue.Date value ->
               [ JProperty(sprintf "%s@odata.type" name, "Edm.DateTime")
                 JProperty(name, value) ]
           | FieldValue.Bool value -> [ JProperty(name, value) ]
           | FieldValue.Binary value ->
               [ JProperty(sprintf "%s@odata.type" name, "Edm.Binary")
                 JProperty(name, value) ]
           )
      |> List.collect id

    fields

  let fromJObject (jObject: JObject): TableFields =
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
              | _ -> p.Value.Value<string>() |> FieldValue.String))
    |> dict
    |> Dictionary

module TableRow =
  open Newtonsoft.Json.Linq

  let toJObject ({ Keys = tablekeys; Fields = tableFields }) =
    let requiredFields =
      [ JProperty("PartitionKey", tablekeys.PartitionKey)
        JProperty("RowKey", tablekeys.RowKey) ]

    let fields = TableFields.toJProperties tableFields

    JObject(fields |> List.append requiredFields)
