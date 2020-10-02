module FilterApplier

open Domain
open System.Collections.Generic

let rec applyFilter (rows: IDictionary<TableKeys, TableFields>) filter =

  let compareFields left qc right =

    let fieldsEqual (left, right) = left = right

    let fieldsGreaterThan (left, right) =
      match left, right with
      | FieldValue.Int left, FieldValue.Int right -> left > right
      | FieldValue.Long left, FieldValue.Long right -> left > right
      | FieldValue.Date left, FieldValue.Date right -> left > right
      | FieldValue.Guid left, FieldValue.Guid right -> left > right
      | FieldValue.Binary left, FieldValue.Binary right -> left > right
      | FieldValue.Bool left, FieldValue.Bool right -> left > right
      | FieldValue.Double left, FieldValue.Double right -> left > right
      | _, _ -> left.AsString > right.AsString

    let fieldsGreaterThanOrEqual (left, right) =
      match left, right with
      | FieldValue.Int left, FieldValue.Int right -> left >= right
      | FieldValue.Long left, FieldValue.Long right -> left >= right
      | FieldValue.Date left, FieldValue.Date right -> left >= right
      | FieldValue.Guid left, FieldValue.Guid right -> left >= right
      | FieldValue.Binary left, FieldValue.Binary right -> left >= right
      | FieldValue.Bool left, FieldValue.Bool right -> left >= right
      | FieldValue.Double left, FieldValue.Double right -> left >= right
      | _, _ -> left.AsString >= right.AsString

    let fieldsLessThan (left, right) =
      match left, right with
      | FieldValue.Int left, FieldValue.Int right -> left < right
      | FieldValue.Long left, FieldValue.Long right -> left < right
      | FieldValue.Date left, FieldValue.Date right -> left < right
      | FieldValue.Guid left, FieldValue.Guid right -> left < right
      | FieldValue.Binary left, FieldValue.Binary right -> left < right
      | FieldValue.Bool left, FieldValue.Bool right -> left < right
      | FieldValue.Double left, FieldValue.Double right -> left < right
      | _, _ -> left.AsString < right.AsString

    let fieldsLessThanOrEqual (left, right) =
      match left, right with
      | FieldValue.Int left, FieldValue.Int right -> left <= right
      | FieldValue.Long left, FieldValue.Long right -> left <= right
      | FieldValue.Date left, FieldValue.Date right -> left <= right
      | FieldValue.Guid left, FieldValue.Guid right -> left <= right
      | FieldValue.Binary left, FieldValue.Binary right -> left <= right
      | FieldValue.Bool left, FieldValue.Bool right -> left <= right
      | FieldValue.Double left, FieldValue.Double right -> left <= right
      | _, _ -> left.AsString <= right.AsString

    match qc with
    | QueryComparison.Equal -> fieldsEqual (left, right)
    | QueryComparison.NotEqual -> fieldsEqual (left, right) |> not
    | QueryComparison.GreaterThan -> fieldsGreaterThan (left, right)
    | QueryComparison.GreaterThanOrEqual -> fieldsGreaterThanOrEqual (left, right)
    | QueryComparison.LessThan -> fieldsLessThan (left, right)
    | QueryComparison.LessThanOrEqual -> fieldsLessThanOrEqual (left, right)

  let toSeq (dictionary : KeyValuePair<_, _> seq) =
    dictionary |> Seq.map (fun kvp -> kvp.Key, kvp.Value)

  match filter with
  | Filter.PartitionKey(qc, pk) ->
      rows |> toSeq |> Seq.filter (fun (keys, _) -> compareFields (FieldValue.String keys.PartitonKey) qc (FieldValue.String pk))
  | Filter.RowKey(qc, rk) ->
      rows |> toSeq |> Seq.filter (fun (keys, _) -> compareFields (FieldValue.String keys.RowKey) qc (FieldValue.String rk))
  | Filter.Property(name, qc, value) ->
      rows
      |> toSeq
      |> Seq.filter (fun (_, values) ->
           values
           |> toSeq
           |> Seq.tryFind (fun (n, _) -> n = name)
           |> function
           | Some (_, v) -> (compareFields v qc value)
           | _ -> false)
  | Filter.Combined(left, tableOperator, right) ->
      match tableOperator with
      | TableOperators.And ->
          let results = applyFilter rows left |> dict
          applyFilter results right
      | TableOperators.Or ->
          let leftResults = applyFilter rows left |> Set.ofSeq
          let rightResults = applyFilter rows right |> Set.ofSeq
          leftResults
          |> Set.union rightResults
          |> Set.toSeq
