module HttpBatch

open Http
open Domain
open System

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
  match writeCommandResponse with
  | WriteCommandResponse.Ack (keys, etag) ->
      { StatusCode = StatusCode.NoContent
        Headers =
          [ "X-Content-Type-Options", "nosniff"
            "Cache-Control", "no-cache"
            "Preference-Applied", "return-no-content"
            "DataServiceVersion", "3.0;"
            "Location", sprintf "http://localhost/devstoreaccount1/test(PartitionKey='%s',RowKey='%s')" keys.PartitionKey keys.RowKey   // TODO: Work out how to get host name and port into this?
            "DataServiceId", sprintf "http://localhost/devstoreaccount1/test(PartitionKey='%s',RowKey='%s')" keys.PartitionKey keys.RowKey
            "ETag", etag |> ETag.toText
            ]
          |> dict
        Body = "" }
  | WriteCommandResponse.Conflict _ ->
      { StatusCode = StatusCode.Conflict
        Headers =
          [ "X-Content-Type-Options", "nosniff"
            "Cache-Control", "no-cache"
            "Preference-Applied", "return-no-content"
            "DataServiceVersion", "3.0;" ]
          |> dict
        Body = "" }



let toHttpResponse (batchResponse: BatchCommandResponse): Http.Response =


  let batchId = Guid.NewGuid().ToString()
  let changesetId = Guid.NewGuid().ToString()
  let header = batchHeader batchId changesetId
  let footer = batchFooter batchId changesetId
  let seperator = changesetSeperator changesetId
  let blankLine = "\n\n"

  let main =
    batchResponse.CommandResponses
    |> List.map (toResponse >> Response.toRaw)
    |> List.map (sprintf "%s%s%s" seperator blankLine)
    |> fun line -> String.Join(sprintf "%s" blankLine, line)

  let body =
    sprintf "%s%s%s%s%s\n" header blankLine main blankLine footer

  { StatusCode = StatusCode.Accepted
    Headers =
      [ "Cache-Control", "no-cache"
        "Content-Type", sprintf "multipart/mixed; boundary=batchresponse_%s" batchId
        "X-Content-Type-Options", "nosniff" ]
      |> dict
    Body = body }