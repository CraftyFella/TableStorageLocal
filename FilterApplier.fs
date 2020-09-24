module FilterApplier

open Domain

let rec applyFilter (rows: TableRow seq) filter =

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

  match filter with
  | Filter.PartionKey(qc, pk) ->
      rows |> Seq.filter (fun r -> compareFields (FieldValue.String r.PartitonKey) qc (FieldValue.String pk))
  | Filter.RowKey(qc, rk) ->
      rows |> Seq.filter (fun r -> compareFields (FieldValue.String r.RowKey) qc (FieldValue.String rk))
  | Filter.Property(name, qc, value) ->
      rows
      |> Seq.filter (fun r ->
           r.Fields
           |> List.tryFind (fun f -> f.Name = name)
           |> function
           | Some field -> (compareFields field.Value qc value)
           | _ -> false)
  | Filter.Combined(left, tableOperator, right) ->
      match tableOperator with
      | TableOperators.And ->
          let results = applyFilter rows left
          applyFilter results right
      | TableOperators.Or ->
          let leftResults = applyFilter rows left |> Set.ofSeq
          let rightResults = applyFilter rows right |> Set.ofSeq
          leftResults
          |> Set.union rightResults
          |> Set.toSeq
