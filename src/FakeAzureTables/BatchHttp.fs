module BatchHttp

open Http
open Domain
open System
open System.Collections.Generic

let private batchHeader batchId changesetId =
  sprintf """--batchresponse_%s
Content-Type: multipart/mixed; boundary=changesetresponse_%s""" batchId changesetId

let private batchFooter batchId changesetId =
  sprintf """--changesetresponse_%s--
--batchresponse_%s--""" changesetId batchId

let private changesetSeperator changesetId =
  sprintf """--changesetresponse_%s
Content-Type: application/http
Content-Transfer-Encoding: binary""" changesetId

let private toResponse (writeCommandResponse: WriteCommandResponse): Http.Response =
  let statusCode =
    match writeCommandResponse with
    | WriteCommandResponse.Ack -> StatusCode.Accepted
    | WriteCommandResponse.Conflict _ -> StatusCode.Conflict

  { StatusCode = statusCode
    Headers = new Dictionary<string, string>()
    Body = null }

let toHttpResponse (batchResponse: BatchCommandResponse): Http.Response =
  let batchId = Guid.NewGuid().ToString "N"
  let changesetId = Guid.NewGuid().ToString "N"
  let header = batchHeader batchId changesetId
  let footer = batchFooter batchId changesetId
  let seperator = changesetSeperator changesetId

  let main =
    batchResponse.CommandResponses
    |> List.map toResponse
    |> List.map Response.toRaw
    |> List.map (sprintf "%s\n%s" seperator)
    |> fun line -> String.Join("\n", line)

  let body =
    sprintf "\n%s\n%s\n%s" header main footer

  { StatusCode = StatusCode.Accepted
    Headers =
      [ "Content-Type", sprintf "multipart/mixed; boundary=batchresponse_%s" batchId ]
      |> dict
    Body = body }
