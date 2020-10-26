namespace FakeAzureTables

[<RequireQualifiedAccess>]
module QueryApplier =

  open Domain
  open LiteDB
  open Bson

  let query (col: ILiteCollection<TableRow>) filter limit (continuation: Continuation option) =

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

    let expression =
      match continuation with
      | Some continuation ->
          let left =
            continuation |> Continuation.toBsonExpression

          Query.And(left, filterExpressionBuilder filter)
      | None -> filterExpressionBuilder filter

    let rows =
      col.Find(expression, limit = limit + 1)
      |> Seq.toArray

    let next =
      if rows.Length = limit + 1 then rows |> Array.tryLast else None

    let continuation =
      match next with
      | Some next ->
          { NextPartitionKey = next.Keys.PartitionKey
            NextRowKey = next.Keys.RowKey }
          |> Some
      | _ -> None

    rows |> Array.truncate limit, continuation
