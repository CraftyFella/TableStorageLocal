open System
open FakeAzureTables

let fakeTablesHost = new Host.FakeTables("data.db", 10002)
Console.ReadLine() |> ignore