namespace TableStorageLocal

#nowarn "25"

[<RequireQualifiedAccess>]
module FilterParser =

  open System
  open Domain
  open FParsec

  [<AutoOpen>]
  module private Parser =

    let letterOrNumber = asciiLetter <|> digit
    let manyLettersOrNumbers = many1Chars letterOrNumber
    let specialChars = many1Chars letterOrNumber
    let manyCharsNotSingleQuote = (many1Chars (noneOf [ '\'' ]))
    let stringSpaces s = pstring s >>. spaces

    module FieldValue =
      let boolParser =
        let ptrue =
          stringReturn "true" (FieldValue.Bool true)

        let pfalse =
          stringReturn "false" (FieldValue.Bool false)

        ptrue <|> pfalse

      let numberFormat =
        NumberLiteralOptions.AllowMinusSign
        ||| NumberLiteralOptions.AllowFraction
        ||| NumberLiteralOptions.AllowExponent
        ||| NumberLiteralOptions.AllowHexadecimal
        ||| NumberLiteralOptions.AllowSuffix

      let numberParser =
        let parser = numberLiteral numberFormat "number"

        fun stream ->
          let reply = parser stream

          if reply.Status = Ok then
            let nl = reply.Result // the parsed NumberLiteral

            if nl.SuffixLength = 0
               || (nl.IsInteger
                   && nl.SuffixLength = 1
                   && nl.SuffixChar1 = 'L') then
              try
                let result =
                  if nl.IsInteger then
                    if nl.SuffixLength = 0 then
                      FieldValue.Int(int nl.String)
                    else
                      FieldValue.Long(int64 nl.String)
                  else if nl.IsHexadecimal then
                    FieldValue.Double(floatOfHexString nl.String)
                  else
                    FieldValue.Double(float nl.String)

                Reply(result)
              with :? System.OverflowException as e ->
                stream.Skip(-nl.String.Length)
                Reply(FatalError, messageError e.Message)
            else
              stream.Skip(-nl.SuffixLength)
              Reply(Error, messageError "invalid number suffix")
          else
            Reply(reply.Status, reply.Error)

      let dateParser =
        let prefix = pstring "datetime"
        let quote = pchar '\''

        prefix
        >>. (between quote quote manyCharsNotSingleQuote
             >>= (fun s ->
               match DateTimeOffset.TryParse s with
               | true, result -> preturn (FieldValue.Date result)
               | _ -> fail "Expected a Date"))

      let binaryParser =
        let prefix = pstring "binary"
        let quote = pchar '\''

        prefix
        >>. (between quote quote (regex "(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?")
             |>> (Convert.FromBase64String >> FieldValue.Binary))

      let guidParser =
        let prefix = pstring "guid"
        let quote = pchar '\''

        prefix
        >>. (between quote quote (regex "(?im)^[{(]?[0-9A-F]{8}[-]?(?:[0-9A-F]{4}[-]?){3}[0-9A-F]{12}[)}]?")
             |>> (Guid.Parse >> FieldValue.Guid))

      let stringParser =
        let quote = pchar '\''

        between quote quote manyCharsNotSingleQuote
        |>> (FieldValue.String)

      let parser =
        guidParser
        <|> dateParser
        <|> binaryParser
        <|> boolParser
        <|> numberParser
        <|> stringParser

    let queryComparisonParser =
      [ ("eq", QueryComparison.Equal)
        ("ne", QueryComparison.NotEqual)
        ("gt", QueryComparison.GreaterThan)
        ("ge", QueryComparison.GreaterThanOrEqual)
        ("lt", QueryComparison.LessThan)
        ("le", QueryComparison.LessThanOrEqual) ]
      |> List.map (fun (toMatch, qc) -> stringReturn toMatch qc <?> (sprintf "%O" qc))
      |> choice

    module Filter =
      let propertyParser =
        let name =
          manyLettersOrNumbers .>> spaces <?> "Name"

        let queryComparison =
          queryComparisonParser .>> spaces
          <?> "QueryComparison"

        let filterValue = FieldValue.parser <?> "FilterValue"

        name .>>. queryComparison .>>. filterValue
        |>> (fun ((n, qc), fv) -> Filter.Property(n, qc, fv))

      let partitionKeyParser =
        let name = pstringCI "partitionKey"
        let nameAndSpaces = name .>> spaces <?> "Name"

        let queryComparison =
          queryComparisonParser .>> spaces
          <?> "QueryComparison"

        let filterValue =
          FieldValue.stringParser <?> "FilterValue"

        nameAndSpaces >>. queryComparison .>>. filterValue
        |>> (fun (qc, (FieldValue.String (fv))) -> Filter.PartitionKey(qc, fv))

      let rowKeyParser =
        let name = pstringCI "rowkey"
        let nameAndSpaces = name .>> spaces <?> "Name"

        let queryComparison =
          queryComparisonParser .>> spaces
          <?> "QueryComparison"

        let filterValue =
          FieldValue.stringParser <?> "FilterValue"

        nameAndSpaces >>. queryComparison .>>. filterValue
        |>> (fun (qc, (FieldValue.String (fv))) -> Filter.RowKey(qc, fv))


      let opp =
        new OperatorPrecedenceParser<Filter, unit, unit>()

      let parser = opp.ExpressionParser

      opp.AddOperator(
        InfixOperator(
          "and",
          spaces,
          1,
          Associativity.Left,
          (fun left right -> Filter.Combined(left, TableOperators.And, right))
        )
      )

      opp.AddOperator(
        InfixOperator(
          "or",
          spaces,
          2,
          Associativity.Left,
          (fun left right -> Filter.Combined(left, TableOperators.Or, right))
        )
      )

      let nonCombineParsers =
        choice [ partitionKeyParser
                 rowKeyParser
                 propertyParser ]

      let termParser =
        (nonCombineParsers .>> spaces)
        <|> between (stringSpaces "(") (stringSpaces ")") parser

      opp.TermParser <- termParser

      let filter = spaces >>. parser .>> spaces .>> eof

  let parse query =
    match run Filter.filter query with
    | Success (result, _, _) -> result |> Result.Ok
    | Failure (_, error, _) -> error.ToString() |> Result.Error
