module Domain

open System
open System.Collections.Generic
open System.Text.RegularExpressions

let (|Regex|_|) pattern input =
  match Regex.Match(input, pattern) with
  | m when m.Success ->
      m.Groups
      |> Seq.skip 1
      |> Seq.map (fun g -> g.Value)
      |> Seq.toList
      |> Some
  | _ -> None

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


type ETag =
  | All
  | Specific of DateTimeOffset
  | Missing

[<RequireQualifiedAccess>]
module ETag =
  let serialize (input: DateTimeOffset) =
    sprintf "W/\"datetime'%s'\"" (input.ToString("s") + input.ToString(".fffZ"))

  let create () = System.DateTimeOffset.UtcNow |> Specific

  let parse (input: string): ETag =
    match input with
    | "*" -> All
    | Regex "datetime'(.+)'" [ etag ] ->
        match DateTimeOffset.TryParse etag with
        | true, etag -> Specific etag
        | _ -> Missing
    | _ -> Missing

type TableCommand =
  | CreateTable of Table: string
  | ListTables

type WriteCommand =
  | Insert of Table: string * TableRow
  | Replace of Table: string * ETag * TableRow
  | Merge of Table: string * ETag * TableRow
  | Delete of Table: string * ETag * TableKeys
  | InsertOrReplace of Table: string * TableRow
  | InsertOrMerge of Table: string * TableRow

type ReadCommand =
  | Get of Table: string * TableKeys
  | Query of Table: string * Filter * Top: int

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
  | TableList of string seq

type WriteCommandResponse =
  | Ack of TableKeys * ETag
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
               [ JProperty("odata.etag", value |> ETag.serialize)
                 JProperty(name, value) ]
           | FieldValue.Date value ->
               [ JProperty(sprintf "%s@odata.type" name, "Edm.DateTime")
                 JProperty(name, value) ]
           | FieldValue.Bool value -> [ JProperty(name, value) ]
           | FieldValue.Binary value ->
               [ JProperty(sprintf "%s@odata.type" name, "Edm.Binary")
                 JProperty(name, value) ])
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

  let merge (existingRow: TableRow) (row: TableRow) =
    for (KeyValue (key, value)) in existingRow.Fields do
      match row.Fields.ContainsKey key with
      | false -> row.Fields.Add(key, value)
      | _ -> ()
    row

  let withETag (etag: ETag) (row: TableRow) =
    match etag with
    | Specific etag ->
        row.Fields.Remove("Timestamp") |> ignore
        row.Fields.TryAdd("Timestamp", FieldValue.Date etag)
        |> ignore
        row
    | _ -> row

  let (|ExistsWithMatchingETag|_|) (existingETag: ETag) (existingRow: TableRow option) =
    match existingRow, existingETag with
    | Some existingRow, ETag.Specific existingETag when (existingRow.ETag |> ETag.serialize) =
                                                          (existingETag |> ETag.serialize) -> Some existingRow
    | Some existingRow, ETag.All -> Some existingRow
    | _ -> None

  let (|ExistsWithDifferentETag|_|) (existingETag: ETag) (existingRow: TableRow option) =
    match existingRow, existingETag with
    | Some existingRow, ETag.Specific existingETag when (existingRow.ETag |> ETag.serialize)
                                                        <> (existingETag |> ETag.serialize) -> Some existingRow
    | _ -> None

module Result =
  let isOk result =
    match result with
    | Ok _ -> true
    | _ -> false

  let valueOf result =
    match result with
    | Ok v -> v
    | _ -> failwithf "shouldn't get here"

module Option =

  let valueOf result =
    match result with
    | Some v -> v
    | _ -> failwithf "shouldn't get here"

module WriteCommand =

  let isWriteCommand command =
    match command with
    | Write c -> true
    | _ -> false

  let valueOf command =
    match command with
    | Write c -> c
    | _ -> failwithf "shouldn't get here"
