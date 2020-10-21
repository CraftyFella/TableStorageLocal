[<RequireQualifiedAccess>]
module HttpRequest

open System.Collections.Generic
open System
open System.Text
open System.Buffers
open Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
open Http
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.Extensions
open System.Text.RegularExpressions
open Domain

let private toMethod m =
  Enum.Parse(typeof<Method>, m, true) :?> Method

type InvalidReason =
  | InvalidRoute of string
  | InvalidRequestHeader of string
  | InvalidMethod
  | Invalid of Exception

let private toReason (ex: Exception) =
  match ex with
  | :? Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException as ex when ex.Message.StartsWith
                                                                                     ("Unrecognized HTTP version") ->
      InvalidRoute ex.Message
  | :? Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException as ex -> InvalidRequestHeader ex.Message
  | :? System.ArgumentException as ex when ex.Message = "Requested value 'Custom' was not found." -> InvalidMethod
  | _ -> Invalid ex

type private ParserCallbacks() =
  let _headers = new Dictionary<string, string array>()
  let mutable _method = Method.Get
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

      _headers.Add(toString(name).ToLower(), [| toString (value) |])

  member __.Headers = _headers
  member __.Method = _method
  member __.Path = _path
  member __.Uri = _uri
  member __.Query = _query


let private normalise (input: string) =
  let normalizedLineEndings =
    input.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n").TrimStart()

  if ("MERGE".Equals(normalizedLineEndings.Substring(0, 5)))
  then sprintf "%s%s" "POST" (normalizedLineEndings.Substring(5))
  else normalizedLineEndings

let parse (input: string) =
  try

    let requestRaw =
      input |> normalise |> Encoding.UTF8.GetBytes

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
        Path =
          if uri.IsAbsoluteUri
          then uri.AbsolutePath
          else uri.OriginalString |> Net.WebUtility.UrlDecode
        Body = body
        Headers = app.Headers
        Query = app.Query
        Uri = uri }

    Result.Ok request
  with ex -> toReason ex |> Error

let tryExtractBatches (request: Request): Request list option =
  match (request.Headers.Item "content-type")
        |> Array.head with
  | Regex "boundary=(.+)$" [ boundary ] ->
      let rawRequests =
        Regex.Split(request.Body, "--changeset_.+$", RegexOptions.Multiline ||| RegexOptions.IgnoreCase)
        |> Array.filter (fun x -> not (x.Contains boundary))
        |> Array.map (fun x ->
             Regex.Split(x, "^content-transfer-encoding: binary", RegexOptions.Multiline ||| RegexOptions.IgnoreCase)
             |> Array.skip 1
             |> Array.head)

      let httpRequests =
        rawRequests |> Array.map (parse) |> List.ofArray

      match httpRequests |> List.forall Result.isOk with
      | true -> httpRequests |> List.map Result.valueOf |> Some
      | false -> None
  | _ -> None

let toRequest (request: HttpRequest): Request =
  let request: Request =
    { Method = request.Method |> toMethod
      Path = request.Path.Value |> Net.WebUtility.UrlDecode
      Body = request.BodyString
      Headers =
        request.Headers
        |> Seq.map (fun (KeyValue (k, v)) -> k.ToLower(), v.ToArray())
        |> dict
      Query =
        request.Query
        |> Seq.map (fun (KeyValue (k, v)) -> k, v.ToArray())
        |> dict
      Uri = request.GetDisplayUrl() |> Uri }

  request
