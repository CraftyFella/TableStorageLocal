module FilterApplier

open Domain
open LiteDB
open Bson

let rec applyFilter (col: ILiteCollection<TableRow>) filter =

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

  let expression = filterExpressionBuilder filter
  col.Find expression
