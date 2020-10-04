module Host

open System
open System.Collections.Generic
open Microsoft.AspNetCore.Hosting
open System.Net.Sockets
open System.Net
open Microsoft.AspNetCore.Builder
open Http
open CommandHandler

let private findPort () =
  TcpListener(IPAddress.Loopback, 0)
  |> fun l ->
       l.Start()
       (l, (l.LocalEndpoint :?> IPEndPoint).Port)
       |> fun (l, p) ->
            l.Stop()
            p

let private app tables (appBuilder: IApplicationBuilder) =
  let inner = httpHandler (commandHandler tables)
  appBuilder.Run(fun ctx -> exceptonLoggingHttpHandler inner ctx)

type FakeTables() =
  let tables = Dictionary<string, _>()
  let port = findPort ()
  // let port = 10002
  let url = sprintf "http://127.0.0.1:%i" port

  let connectionString =
    sprintf
      "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;TableEndpoint=http://localhost.charlesproxy.com:%i/devstoreaccount1;"
      port

  let webHost =
    WebHostBuilder().Configure(fun appBuilder -> app tables appBuilder).UseUrls(url)
      .UseKestrel(fun options -> options.AllowSynchronousIO <- true).Build()

  do webHost.Start()

  member __.ConnectionString =
    connectionString

  member __.Tables = tables

  interface IDisposable with
    member __.Dispose() = webHost.Dispose()
