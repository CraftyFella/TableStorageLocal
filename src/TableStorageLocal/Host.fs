namespace TableStorageLocal

open LiteDB
open LiteDB.Engine
open System
open Microsoft.AspNetCore.Hosting

[<AutoOpen>]
module private Host =

  open System.Net.Sockets
  open System.Net
  open Microsoft.AspNetCore.Builder
  open HttpContext
  open CommandHandler

  let findPort () =
    TcpListener(IPAddress.Loopback, 0)
    |> fun l ->
         l.Start()
         (l, (l.LocalEndpoint :?> IPEndPoint).Port)
         |> fun (l, p) ->
              l.Stop()
              p

  let app db (appBuilder: IApplicationBuilder) =
    let inner = commandHandler db |> httpHandler
    appBuilder.Run(fun ctx -> exceptionLoggingHttpHandler inner ctx)

type LocalTables(connectionString: string, port: int) =

  let db =
    new LiteDatabase(connectionString, Bson.FieldValue.mapper ())

  let url = sprintf "http://127.0.0.1:%i" port

  let mutable connectionString =
    sprintf
      "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;TableEndpoint=http://%s:%i/devstoreaccount1;"
      (if Environment.GetEnvironmentVariable("TABLESTORAGELOCAL_USEPROXY")
          <> null then
        "localhost.charlesproxy.com"
       else
         "localhost")
      port

  let webHost =
    WebHostBuilder().Configure(fun appBuilder -> app db appBuilder).UseUrls(url)
      .UseKestrel(fun options -> options.AllowSynchronousIO <- true).Build()

  do
    // Case Sensitive
    db.Rebuild(RebuildOptions(Collation = Collation.Binary))
    |> ignore
    if Environment.GetEnvironmentVariable("TABLESTORAGELOCAL_CONNECTIONSTRING")
       <> null then
      connectionString <- Environment.GetEnvironmentVariable("TABLESTORAGELOCAL_CONNECTIONSTRING")
    else
      webHost.Start()

  new() = new LocalTables("filename=:memory:", findPort ())
  new(connectionString: string) = new LocalTables(connectionString, findPort ())
  new(port: int) = new LocalTables("filename=:memory:", port)

  member __.ConnectionString = connectionString

  interface IDisposable with
    member __.Dispose() =
      db.Dispose()
      webHost.Dispose()
