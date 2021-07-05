module insert_entity_concurrently

open Expecto
open System
open Microsoft.Azure.Cosmos.Table
open System.Threading.Tasks

[<Tests>]
let insertTests =
  testList
    "insert_entity_concurrently"
    [ test "rows are inserted" {
        let table = createLocalTables ()

        let tasks =
          Array.init
            10
            (fun _ ->
              DynamicTableEntity("pk", Guid.NewGuid().ToString(), "*", allFieldTypes ())
              |> TableOperation.Insert
              |> table.ExecuteAsync)

        let actual =
          Task.WhenAll(tasks).Result
          |> Array.map (fun result -> result.HttpStatusCode)

        Expect.allEqual actual 204 "unexpected result"
      } ]
