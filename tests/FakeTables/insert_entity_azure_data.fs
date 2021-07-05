module insert_entity_azure_data

open Expecto
open Azure.Data.Tables

[<Tests>]
let insertTests =
  testList
    "Insert"
    [ test "row doesn't exist is accepted for azure data client" {
        let tableClient = createAzureDataTableClient ()
        let fields = allFieldTypesDict
        fields.Add("PartitionKey", "pk2")
        fields.Add("RowKey", "r2k")

        let actual =
          TableEntity fields |> tableClient.AddEntity

        Expect.equal actual.Status 204 "unexpected result"

        let actual =
          tableClient.GetEntity<TableEntity>("pk2", "r2k")

        Expect.equal (actual.GetRawResponse().Status) 200 "unexpected result"

        for field in fields do
          Expect.equal actual.Value.[field.Key] field.Value "unexpected value"
      } ]
