module ParserTests

open Expecto
open Domain
open System

[<Tests>]
let parserTests =
  testList
    "ParserTests"
    [ test "partition key" {

        let actual =
          "PartitionKey eq 'pk'" |> FilterParser.parse

        let expected =
          Ok(Filter.PartitionKey(QueryComparison.Equal, "pk"))

        Expect.equal actual expected "unexpected result"
      }

      test "partition key with dashes" {

        let actual =
          "PartitionKey eq 'a70cf19c-b076-40bb-b3c3-682b74981ba6'" |> FilterParser.parse

        let expected =
          Ok(Filter.PartitionKey(QueryComparison.Equal, "a70cf19c-b076-40bb-b3c3-682b74981ba6"))

        Expect.equal actual expected "unexpected result"
      }

      test "row key" {

        let actual = "RowKey eq 'rk'" |> FilterParser.parse

        let expected =
          Ok(Filter.RowKey(QueryComparison.Equal, "rk"))

        Expect.equal actual expected "unexpected result"
      }

      test "property (String)" {

        let actual = "Field eq 'value'" |> FilterParser.parse

        let expected =
          Ok(Filter.Property("Field", QueryComparison.Equal, FieldValue.String "value"))

        Expect.equal actual expected "unexpected result"
      }

      test "property (Long)" {

        let actual = "Field eq 1L" |> FilterParser.parse

        let expected =
          Ok(Filter.Property("Field", QueryComparison.Equal, FieldValue.Long 1L))

        Expect.equal actual expected "unexpected result"
      }

      test "property (Guid)" {

        let actual =
          "Field eq guid'd80d1dea-830a-43ab-bcc9-01386e037469'"
          |> FilterParser.parse

        let expected =
          Ok
            (Filter.Property
              ("Field", QueryComparison.Equal, FieldValue.Guid(Guid.Parse("d80d1dea-830a-43ab-bcc9-01386e037469"))))

        Expect.equal actual expected "unexpected result"
      }

      test "property (Date)" {

        let actual =
          "Field ge datetime'2000-10-01T01:11:22.931Z'"
          |> FilterParser.parse

        let expected =
          Ok
            (Filter.Property
              ("Field",
               QueryComparison.GreaterThanOrEqual,
               FieldValue.Date(DateTimeOffset.Parse("2000-10-01T01:11:22.931Z"))))

        Expect.equal actual expected "unexpected result"
      }

      test "property (Bool)" {

        let actual = "Field eq true" |> FilterParser.parse

        let expected =
          Ok(Filter.Property("Field", QueryComparison.Equal, FieldValue.Bool true))

        Expect.equal actual expected "unexpected result"
      }

      test "property (Int)" {

        let actual = "Field eq 1" |> FilterParser.parse

        let expected =
          Ok(Filter.Property("Field", QueryComparison.Equal, FieldValue.Int 1))

        Expect.equal actual expected "unexpected result"
      }


      test "property (Double)" {

        let actual = "Field eq 1.0" |> FilterParser.parse

        let expected =
          Ok(Filter.Property("Field", QueryComparison.Equal, FieldValue.Double 1.))

        Expect.equal actual expected "unexpected result"
      }

      test "property (Binary)" {

        let actual =
          "Field eq binary'aGVsbG8='" |> FilterParser.parse

        let expected =
          Ok
            (Filter.Property
              ("Field",
               QueryComparison.Equal,
               FieldValue.Binary [| 104uy
                                    101uy
                                    108uy
                                    108uy
                                    111uy |]))

        Expect.equal actual expected "unexpected result"
      }

      test "combined (And)" {

        let left =
          "(PartitionKey eq 'pk') and (RowKey eq 'rk')"
          |> FilterParser.parse

        let expected =
          Ok
            (Filter.Combined
              (Filter.PartitionKey(QueryComparison.Equal, "pk"),
               TableOperators.And,
               Filter.RowKey(QueryComparison.Equal, "rk")))

        Expect.equal left expected "unexpected result"
      }

      test "combined (Or))" {

        let left =
          "(PartitionKey eq 'pk') or (RowKey eq 'rk')"
          |> FilterParser.parse

        let expected =
          Ok
            (Filter.Combined
              (Filter.PartitionKey(QueryComparison.Equal, "pk"),
               TableOperators.Or,
               Filter.RowKey(QueryComparison.Equal, "rk")))

        Expect.equal left expected "unexpected result"
      }

      test "greater than" {

        let actual = "Field gt 1L" |> FilterParser.parse

        let expected =
          Ok(Filter.Property("Field", QueryComparison.GreaterThan, FieldValue.Long 1L))

        Expect.equal actual expected "unexpected result"
      }

      test "greater than or equal" {

        let actual = "Field ge 1L" |> FilterParser.parse

        let expected =
          Ok(Filter.Property("Field", QueryComparison.GreaterThanOrEqual, FieldValue.Long 1L))

        Expect.equal actual expected "unexpected result"
      }

      test "less than" {

        let actual = "Field lt 1L" |> FilterParser.parse

        let expected =
          Ok(Filter.Property("Field", QueryComparison.LessThan, FieldValue.Long 1L))

        Expect.equal actual expected "unexpected result"
      }

      test "less than or equal" {

        let actual = "Field le 1L" |> FilterParser.parse

        let expected =
          Ok(Filter.Property("Field", QueryComparison.LessThanOrEqual, FieldValue.Long 1L))

        Expect.equal actual expected "unexpected result"
      } ]
