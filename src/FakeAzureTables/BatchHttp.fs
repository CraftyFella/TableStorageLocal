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
    | WriteCommandResponse.Ack -> StatusCode.NoContent
    | WriteCommandResponse.Conflict _ -> StatusCode.Conflict

  { StatusCode = statusCode
    Headers = new Dictionary<string, string>()
    Body = null }

let toHttpResponse2 (batchResponse: BatchCommandResponse) (batchId : Guid) (changesetId: Guid) : Http.Response =

  (*

BLANK_LINE
BATCH_HEADER
BLANK_LINE
SEPERATOR
BLANK_LINE
RAW_RESPONSE
BLANK_LINE
BLANK_LINE
SEPERATOR
BLANK_LINE
RAW_RESPONSE
BLANK_LINE
BLANK_LINE
BATCH_FOOTER

  *)


  let batchId = batchId.ToString()
  let changesetId = changesetId.ToString()
  let header = batchHeader batchId changesetId
  let footer = batchFooter batchId changesetId
  let seperator = changesetSeperator changesetId
  let blankLine = "\n\n"

  let main =
    batchResponse.CommandResponses
    |> List.map toResponse
    |> List.map Response.toRaw
    |> List.map (sprintf "%s%s%s" seperator blankLine)
    |> fun line -> String.Join(sprintf "%s" blankLine, line)

  let body =
    sprintf "%s%s%s%s%s" header blankLine main blankLine footer

  { StatusCode = StatusCode.Accepted
    Headers =
      [ "Content-Type", sprintf "multipart/mixed; boundary=batchresponse_%s" batchId ]
      |> dict
    Body = body }

let toHttpResponse (batchResponse: BatchCommandResponse) : Http.Response =
  toHttpResponse2 batchResponse (Guid.NewGuid()) (Guid.NewGuid())