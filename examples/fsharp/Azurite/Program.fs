open TableStorageLocal
open System.Threading

new LocalTables("./data.db", 10002) |> ignore
Thread.Sleep Timeout.Infinite