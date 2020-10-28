open System
open TableStorageLocal

let fakeTablesHost = new LocalTables("data.db", 10002)
Console.ReadLine() |> ignore