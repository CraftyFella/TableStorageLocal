namespace TableStorageLocal

module Http =

  open Microsoft.AspNetCore.Http
  open System.IO
  open System.Collections.Generic
  open System
  open System.Text

  type HttpRequest with
    member __.BodyString = (new StreamReader(__.Body)).ReadToEnd()

  [<RequireQualifiedAccess>]
  type internal Headers = (string * string) seq

  [<RequireQualifiedAccess>]
  type internal Body = string

  [<RequireQualifiedAccess>]
  type Method =
    | Delete = 0
    | Get = 1
    | Head = 2
    | Options = 3
    | Post = 4
    | Put = 5
    | Trace = 6
    | Patch = 7

  [<RequireQualifiedAccess>]
  type Request =
    { Method: Method
      Path: string
      Body: string
      Headers: IDictionary<string, string array>
      Query: IDictionary<string, string array>
      Uri: Uri }

  [<RequireQualifiedAccess>]
  type StatusCode =
    | Ok = 200
    | NoContent = 204
    | Accepted = 202
    | BadRequest = 400
    | NotFound = 404
    | Conflict = 409
    | PreconditionFailed = 412
    | InternalServerError = 500

  module StatusCode =
    let toRaw (statusCode: StatusCode) =
      match statusCode with
      | StatusCode.Ok -> "HTTP/1.1 200 Ok"
      | StatusCode.NoContent -> "HTTP/1.1 204 No Content"
      | StatusCode.Accepted -> "HTTP/1.1 202 Accepted"
      | StatusCode.BadRequest -> "HTTP/1.1 400 Bad Request"
      | StatusCode.NotFound -> "HTTP/1.1 404 Not Found"
      | StatusCode.Conflict -> "HTTP/1.1 409 Conflict"
      | StatusCode.PreconditionFailed -> "HTTP/1.1 412 Precondition Failed"
      | StatusCode.InternalServerError -> "HTTP/1.1 500 Internal Server Error"

  [<RequireQualifiedAccess>]
  type ContentType =
    | ApplicationJson
    | ApplicationJsonODataStreaming
    | MultipartMixedBatch of string

  module ContentType =
    let toRaw =
      function
      | ContentType.ApplicationJson -> "application/json; charset=utf-8"
      | ContentType.ApplicationJsonODataStreaming ->
          "application/json;odata=minimalmetadata;streaming=true;charset=utf-8"
      | ContentType.MultipartMixedBatch batchId -> sprintf "multipart/mixed; boundary=batchresponse_%s" batchId

  [<RequireQualifiedAccess>]
  type Response =
    { StatusCode: StatusCode
      ContentType: ContentType option
      Headers: IDictionary<string, string>
      Body: string option }

  module Response =
    let toRaw (response: Response) =
      let rawStatusCode = response.StatusCode |> StatusCode.toRaw

      let headers =
        response.Headers
        |> Seq.map (fun kvp -> sprintf "%s: %s" kvp.Key kvp.Value)
        |> fun h -> String.Join("\n", h)

      let sb = StringBuilder()
      sb.Append rawStatusCode |> ignore
      sb.AppendLine() |> ignore
      sb.Append headers |> ignore
      response.ContentType
      |> Option.iter (fun cont ->
           sb.Append
             (sb.AppendLine() |> ignore
              sprintf "Content-Type: %s" (cont |> ContentType.toRaw))
           |> ignore)
      sb.AppendLine() |> ignore
      response.Body
      |> Option.iter (fun body ->
           sb.AppendLine() |> ignore
           sb.Append body |> ignore)
      let raw = sb.ToString()
      raw
