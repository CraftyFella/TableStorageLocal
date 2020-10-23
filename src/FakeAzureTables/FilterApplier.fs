module FilterApplier

open Domain
open LiteDB
open Bson

let rec applyFilter (col: ILiteCollection<TableRow>) filter limit (continuation: Continuation option) =

  let queryComparisonExpressionBuilder field qc value =
    match qc with
    | QueryComparison.Equal -> Query.EQ(field, FieldValue.toBsonValue value)
    | QueryComparison.NotEqual -> Query.Not(field, FieldValue.toBsonValue value)
    | QueryComparison.GreaterThan -> Query.GT(field, FieldValue.toBsonValue value)
    | QueryComparison.GreaterThanOrEqual -> Query.GTE(field, FieldValue.toBsonValue value)
    | QueryComparison.LessThan -> Query.LT(field, FieldValue.toBsonValue value)
    | QueryComparison.LessThanOrEqual -> Query.LTE(field, FieldValue.toBsonValue value)

  let rec filterExpressionBuilder filter =
    match filter with
    | Filter.PartitionKey (qc, pk) -> queryComparisonExpressionBuilder "$.Keys.PartitionKey" qc (FieldValue.String pk)
    | Filter.RowKey (qc, rk) -> queryComparisonExpressionBuilder "$.Keys.RowKey" qc (FieldValue.String rk)
    | Filter.Property (name, qc, value) -> queryComparisonExpressionBuilder (sprintf "$.Fields.%s" name) qc value
    | Filter.Combined (left, tableOperator, right) ->
        let leftExpression = filterExpressionBuilder left
        let rightExpression = filterExpressionBuilder right
        match tableOperator with
        | TableOperators.And -> Query.And(leftExpression, rightExpression)
        | TableOperators.Or -> Query.Or(leftExpression, rightExpression)
    | Filter.All ->
        queryComparisonExpressionBuilder "$.Keys.PartitionKey" QueryComparison.NotEqual (FieldValue.String "--")

  // let applyContinuation (expr: BsonExpression) =
  //   match continuation with
  //   | Some continuation ->
  //       let continuationExpression =
  //         Query.And
  //           (Query.GTE("$.Keys.PartitionKey", BsonValue continuation.NextPartitionKey),
  //            Query.GTE("$.Keys.RowKey", BsonValue continuation.NextRowKey))

  //       Query.And(continuationExpression, expr)
  //   | _ -> expr

  let expression = filterExpressionBuilder filter

  let skip =
    continuation
    |> Option.map (fun c -> c.NextPartitionKey)
    |> Option.map (int)
    |> Option.defaultValue 0

  let rows =
    col.Find(expression, limit = limit, skip = skip)
    |> Seq.toArray

  let continuation =
    match rows.Length > 0 with
    | true ->
        { NextPartitionKey = string (rows.Length + skip)
          NextRowKey = string (rows.Length + skip) }
        |> Some
    | _ -> None

  // printfn "Continuation %A" continuation
  rows, continuation
