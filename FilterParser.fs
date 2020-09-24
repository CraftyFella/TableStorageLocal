module FilterParser

open System
open Domain
open FParsec

let fvBool =
    let btrue =
        stringReturn "true" (FieldValue.Bool true)

    let bfalse =
        stringReturn "false" (FieldValue.Bool false)

    btrue <|> bfalse

let fvLong =
    pint64 .>> pchar 'L' |>> (FieldValue.Long)

let fvInt = pint32 |>> (FieldValue.Int)

let fvDate =
    let prefix = pstring "datetime"
    let quote = pchar '\''
    prefix
    >>. (between quote quote (regex "\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d+Z")
         |>> (DateTimeOffset.Parse >> FieldValue.Date))

let fvGuid =
    let prefix = pstring "guid"
    let quote = pchar '\''
    prefix
    >>. (between quote quote (regex "(?im)^[{(]?[0-9A-F]{8}[-]?(?:[0-9A-F]{4}[-]?){3}[0-9A-F]{12}[)}]?")
         |>> (Guid.Parse >> FieldValue.Guid))

let letterOrNumber = asciiLetter <|> digit
let manyLettersOrNumbers = many1Chars letterOrNumber

let fvString =
    let quote = pchar '\''
    between quote quote manyLettersOrNumbers
    |>> FieldValue.String

let fFieldValue =
    fvGuid
    <|> fvDate
    <|> fvBool
    <|> fvLong
    <|> fvInt
    <|> fvString

let fQueryComparison =
    [ ("eq", QueryComparison.Equal)
      ("ne", QueryComparison.NotEqual)
      ("gt", QueryComparison.GreaterThan)
      ("ge", QueryComparison.GreaterThanOrEqual)
      ("lt", QueryComparison.LessThan)
      ("le", QueryComparison.LessThanOrEqual) ]
    |> List.map (fun (toMatch, qc) -> stringReturn toMatch qc <?> (sprintf "%O" qc))
    |> choice

let fProperty =
    let name =
        manyLettersOrNumbers .>> spaces <?> "Name"

    let queryComparison =
        fQueryComparison .>> spaces <?> "QueryComparison"

    let filterValue = fFieldValue <?> "FilterValue"

    name
    .>>. queryComparison
    .>>. filterValue
    |>> (fun ((n, qc), fv) -> Filter.Property(n, qc, fv))

let fTableOperators =
    [ ("or", TableOperators.Or)
      ("and", TableOperators.And) ]
    |> List.map (fun (toMatch, tableOp) ->
        stringReturn toMatch tableOp
        <?> (sprintf "%O" tableOp))
    |> choice

let fFilter, fFilterRef = createParserForwardedToRef ()

let fcombined =
    let open' = pchar '('
    let close = pchar ')'

    let left = (between open' close fFilter) .>> spaces
    let right = spaces >>. (between open' close fFilter)
    (left .>>. fTableOperators .>>. right)
    |>> (fun ((a, tableOp), b) -> Filter.Combined(a, tableOp, b))

let fPartitionKey =
    let name = pstringCI "partitionKey"
    let nameAndSpaces = name .>> spaces <?> "Name"

    let queryComparison =
        fQueryComparison .>> spaces <?> "QueryComparison"

    let filterValue = fvString <?> "FilterValue"

    nameAndSpaces
    >>. queryComparison
    .>>. filterValue
    |>> (fun (qc, (FieldValue.String (fv))) -> Filter.PartionKey(qc, fv))

let fRowKey =
    let name = pstringCI "rowkey"
    let nameAndSpaces = name .>> spaces <?> "Name"

    let queryComparison =
        fQueryComparison .>> spaces <?> "QueryComparison"

    let filterValue = fvString <?> "FilterValue"

    nameAndSpaces
    >>. queryComparison
    .>>. filterValue
    |>> (fun (qc, (FieldValue.String (fv))) -> Filter.RowKey(qc, fv))

do fFilterRef
   := choice [ fPartitionKey
               fRowKey
               fProperty
               fcombined  ]

let filter = spaces >>. fFilter .>> spaces .>> eof

let fParse query =
    match run filter query with
    | Success (result, _, _) -> result |> Result.Ok
    | Failure (_, error, _) -> error |> Result.Error
