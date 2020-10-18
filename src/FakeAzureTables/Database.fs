module Database
open System
open LiteDB
open System.Collections.Generic
open Domain
open System.Text.RegularExpressions

let randomString =
  let chars =
    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"

  let random = Random()

  let generate length =
    Array.init length (fun _ ->
      (random.Next(0, chars.Length - 1), chars)
      ||> Seq.item)
    |> String

  generate

type ILiteDatabase with

  member __.TablesCollection =
    let col =
      __.GetCollection<KeyValuePair<string, string>> "_tables"

    col.EnsureIndex(fun kvp -> kvp.Key) |> ignore
    col

  member __.GetCollectionName table =
    __.TablesCollection.Find(fun kvp -> kvp.Key = table)
    |> Seq.tryHead
    |> Option.map (fun kvp -> kvp.Value)

  member __.TableExists table =
    __.GetCollectionName table |> Option.isSome

  member __.GetTable table =
    let collectionName =
      match __.GetCollectionName table with
      | Some collectionName -> collectionName
      | _ ->
          let kvp = KeyValuePair(table, randomString 12)
          __.TablesCollection.Insert kvp |> ignore
          kvp.Value

    let col =
      __.GetCollection<TableRow> collectionName

    col.EnsureIndex(fun tableRow -> tableRow.Id)
    |> ignore
    col

type ILiteCollection<'T> with
  member __.TryInsert(row: 'T) =
    try
      __.Insert row |> ignore
      true
    with
    | :? LiteException as ex when ex.ErrorCode = LiteException.INDEX_DUPLICATE_KEY -> false
    | _ -> reraise ()

  member __.TryFindOne(predicate: BsonExpression) = __.Find predicate |> Seq.tryHead

let tableNameIsValid tableName =
  Regex.IsMatch(tableName, "^[A-Za-z0-9]{2,62}$")