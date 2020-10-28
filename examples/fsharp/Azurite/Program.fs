open System
open TableStorageLocal

new LocalTables("./data.db", 10002) |> ignore
Console.ReadLine() |> ignore