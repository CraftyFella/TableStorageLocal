open System
open FakeAzureTables

let fakeTablesHost = new FakeTables("data.db", 10002)
Console.ReadLine() |> ignore