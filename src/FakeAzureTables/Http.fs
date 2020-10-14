module Http
open Microsoft.AspNetCore.Http
open System.IO
open System.Collections.Generic
open System

type HttpRequest with
  member __.BodyString = (new StreamReader(__.Body)).ReadToEnd()

[<RequireQualifiedAccess>]
type internal Headers = (string * string) seq

[<RequireQualifiedAccess>]
type internal Body = string

[<RequireQualifiedAccess>]
type Method =
  | DELETE = 0
  | GET = 1
  | HEAD = 2
  | OPTIONS = 3
  | POST = 4
  | PUT = 5
  | TRACE = 6
  | PATCH = 7

[<RequireQualifiedAccess>]
type Request =
  { Method: Method
    Path: string
    Body: string
    Headers: IDictionary<string, string array>
    Query: IDictionary<string, string array>
    Uri: Uri }

let toMethod m =
  Enum.Parse(typeof<Method>, m, true) :?> Method

let toRequest (request: HttpRequest): Request =
  { Method = request.Method |> toMethod
    Path = request.Path.Value
    Body = request.BodyString
    Headers =
      request.Headers
      |> Seq.map (fun (KeyValue (k, v)) -> k, v.ToArray())
      |> dict
    Query =
      request.Query
      |> Seq.map (fun (KeyValue (k, v)) -> k, v.ToArray())
      |> dict
    Uri = request.Path.Value |> Uri }

[<RequireQualifiedAccess>]
module private Parser =
  open System.Buffers
  open System.Text
  open Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http

  type InvalidReason =
    | InvalidRoute of string
    | InvalidRequestHeader of string
    | InvalidMethod
    | Invalid of Exception

  let toReason (ex: Exception) =
    match ex with
    | :? Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException as ex when ex.Message.StartsWith
                                                                                       ("Unrecognized HTTP version") ->
        InvalidRoute ex.Message
    | :? Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException as ex -> InvalidRequestHeader ex.Message
    | :? System.ArgumentException as ex when ex.Message = "Requested value 'Custom' was not found." -> InvalidMethod
    | _ -> Invalid ex

  type private ParserCallbacks() =
    let _headers = new Dictionary<string, string array>()
    let mutable _method = Method.GET
    let mutable _path = ""
    let mutable _uri = ""
    let _query = new Dictionary<string, string array>()

    let toString (input: Span<byte>) = Encoding.UTF8.GetString(input.ToArray())

    interface IHttpRequestLineHandler with
      member this.OnStartLine(method: HttpMethod,
                              version: HttpVersion,
                              target: Span<byte>,
                              path: Span<byte>,
                              query: Span<byte>,
                              customMethod: Span<byte>,
                              pathEncoded: bool) =
        _method <- (string method) |> toMethod
        _path <- toString (path)
        _uri <- toString (target)

        let QueryString =
          Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(toString (query))

        for query in QueryString do
          _query.Add(query.Key, query.Value.ToArray())
        ()


    interface IHttpHeadersHandler with
      member this.OnHeader(name: Span<byte>, value: Span<byte>) =

        _headers.Add(toString (name), [| toString (value) |])

    member __.Headers = _headers
    member __.Method = _method
    member __.Path = _path
    member __.Uri = _uri
    member __.Query = _query

  let parse (input: string) =
    try
      File.WriteAllText("example.http", input.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n").Trim())

      let requestRaw =
        Encoding.UTF8.GetBytes(input.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n").Trim())

      let mutable buffer = new ReadOnlySequence<byte>(requestRaw)
      let parser = new HttpParser<ParserCallbacks>()
      let app = new ParserCallbacks()
      let mutable consumed = Unchecked.defaultof<_>
      let mutable examined = Unchecked.defaultof<_>
      parser.ParseRequestLine(app, &buffer, &consumed, &examined)
      |> ignore
      buffer <- buffer.Slice(consumed)
      let mutable consumedBytes = Unchecked.defaultof<_>
      parser.ParseHeaders(app, &buffer, &consumed, &examined, &consumedBytes)
      |> ignore
      buffer <- buffer.Slice(consumed)

      let body =
        Encoding.UTF8.GetString(buffer.ToArray())
      let uri = Uri(app.Uri, UriKind.RelativeOrAbsolute)
      let request: Request =
        { Method = app.Method
          Path = uri.AbsolutePath
          Body = body
          Headers = app.Headers
          Query = app.Query
          Uri = uri }

      Result.Ok request
    with ex -> toReason ex |> Error