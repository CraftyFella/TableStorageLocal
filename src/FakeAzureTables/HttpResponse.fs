namespace FakeAzureTables

[<RequireQualifiedAccess>]
module HttpResponse =

  open Http
  open Domain
  open System
  open Newtonsoft.Json.Linq
  open System.Collections.Generic
  open Newtonsoft.Json
  open Microsoft.AspNetCore.Http
  open Microsoft.Extensions.Primitives
  open FSharp.Control.Tasks

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

  let private fromWriteCommandResponse (response: WriteCommandResponse): Http.Response =
    match response with
    | WriteCommandResponse.Ack (keys, etag) ->
        let headers =
          [ "X-Content-Type-Options", "nosniff"
            "Cache-Control", "no-cache"
            "Preference-Applied", "return-no-content"
            "DataServiceVersion", "3.0;"
            "Location",
            sprintf
              "http://localhost/devstoreaccount1/test(PartitionKey='%s',RowKey='%s')"
              keys.PartitionKey
              keys.RowKey // TODO: Work out how to get host name and port into this?
            "DataServiceId",
            sprintf
              "http://localhost/devstoreaccount1/test(PartitionKey='%s',RowKey='%s')"
              keys.PartitionKey
              keys.RowKey ]

        let headers =
          match etag with
          | Specific etag ->
              headers
              |> List.append [ "ETag", etag |> ETag.serialize ]
          | _ -> headers

        { StatusCode = StatusCode.NoContent
          ContentType = None
          Headers = headers |> dict
          Body = None }
    | WriteCommandResponse.Conflict UpdateConditionNotSatisfied ->
        { StatusCode = StatusCode.PreconditionFailed
          ContentType = None
          Headers =
            [ "X-Content-Type-Options", "nosniff"
              "Cache-Control", "no-cache"
              "Preference-Applied", "return-no-content"
              "DataServiceVersion", "3.0;" ]
            |> dict
          Body = None }
    | WriteCommandResponse.Conflict EntityAlreadyExists ->
        let now = DateTimeOffset.UtcNow

        let timestamp =
          now.ToString("s") + now.ToString(".fffZ")

        { StatusCode = StatusCode.Conflict
          ContentType = Some ContentType.ApplicationJson
          Headers =
            [ "X-Content-Type-Options", "nosniff"
              "Cache-Control", "no-cache"
              "Preference-Applied", "return-no-content"
              "DataServiceVersion", "3.0;" ]
            |> dict
          Body =
            Some
              (sprintf
                "{\"odata.error\":{\"code\":\"EntityAlreadyExists\",\"message\":{\"lang\":\"en-US\",\"value\":\"The specified entity already exists.\\nRequestId:8c265fa5-9002-005c-5b56-ac59bc000000\\nTime:%s\"}}}"
                 timestamp) }
    | WriteCommandResponse.Conflict ResourceNotFound ->
        let now = DateTimeOffset.UtcNow

        let timestamp =
          now.ToString("s") + now.ToString(".fffZ")

        { StatusCode = StatusCode.NotFound
          ContentType = Some ContentType.ApplicationJson
          Headers =
            [ "X-Content-Type-Options", "nosniff"
              "Cache-Control", "no-cache"
              "Preference-Applied", "return-no-content"
              "DataServiceVersion", "3.0;" ]
            |> dict
          Body =
            Some
              (sprintf
                "{\"odata.error\":{\"code\":\"ResourceNotFound\",\"message\":{\"lang\":\"en-US\",\"value\":\"The specified resource does not exist.\\nRequestId:8c265fa5-9002-005c-5b56-ac59bc000000\\nTime:%s\"}}}"
                 timestamp) }

  let private fromBatchCommandResponse (response: BatchCommandResponse): Http.Response =

    let batchId = Guid.NewGuid().ToString()
    let changesetId = Guid.NewGuid().ToString()
    let header = batchHeader batchId changesetId
    let footer = batchFooter batchId changesetId
    let seperator = changesetSeperator changesetId
    let blankLine = "\n\n"

    let responses =
      match response with
      | WriteResponses responses -> responses |> List.map (fromWriteCommandResponse)
      | BadRequest reason ->
          let now = DateTimeOffset.UtcNow

          let timestamp =
            now.ToString("s") + now.ToString(".fffZ")

          [ { StatusCode = StatusCode.BadRequest
              ContentType = Some ContentType.ApplicationJsonODataStreaming
              Headers =
                [ "X-Content-Type-Options", "nosniff"
                  "Cache-Control", "no-cache"
                  "Preference-Applied", "return-no-content"
                  "DataServiceVersion", "3.0;" ]
                |> dict
              Body =
                Some
                  (sprintf
                    "{\"odata.error\":{\"code\":\"InvalidDuplicateRow\",\"message\":{\"lang\":\"en-US\",\"value\":\"1:The batch request contains multiple changes with same row key. An entity can appear only once in a batch request.\\nRequestId:b61748bc-6002-0006-1f59-ac5f3d000000\\nTime:%s\"}}}"
                     timestamp) } ]

    let main =
      responses
      |> List.map Response.toRaw
      |> List.map (sprintf "%s%s%s" seperator blankLine)
      |> fun line -> String.Join(sprintf "%s" blankLine, line)

    let body =
      sprintf "%s%s%s%s%s\n" header blankLine main blankLine footer

    { StatusCode = StatusCode.Accepted
      ContentType = Some(ContentType.MultipartMixedBatch batchId)
      Headers =
        [ "Cache-Control", "no-cache"
          "X-Content-Type-Options", "nosniff" ]
        |> dict
      Body = Some body }

  let private fromReadCommandResponse (response: ReadCommandResponse): Http.Response =
    match response with
    | GetResponse response ->
        let jObject = response |> TableRow.toJObject
        let json = jObject.ToString()
        { StatusCode = StatusCode.Ok
          ContentType = Some ContentType.ApplicationJson
          Headers =
            [ "ETag", response.ETag |> ETag.serialize ]
            |> dict
          Body = Some json }
    | QueryResponse (rows, continuation) ->
        let rows =
          rows |> Array.map (TableRow.toJObject) |> JArray

        let response = JObject([ JProperty("value", rows) ])
        let json = (response.ToString())
        { StatusCode = StatusCode.Ok
          ContentType = Some ContentType.ApplicationJson
          Headers =
            match continuation with
            | None -> []
            | Some continuation ->
                [ "x-ms-continuation-NextPartitionKey", continuation.NextPartitionKey
                  "x-ms-continuation-NextRowKey", continuation.NextRowKey ]
            |> dict
          Body = Some json }
    | ReadCommandResponse.NotFoundResponse ->
        { StatusCode = StatusCode.NotFound
          ContentType = None
          Headers = new Dictionary<_, _>()
          Body = None }

  let private fromTableCommandResponse (response: TableCommandResponse): Http.Response =
    match response with
    | TableCommandResponse.Ack ->
        { StatusCode = StatusCode.NoContent
          ContentType = None
          Headers = new Dictionary<_, _>()
          Body = None }
    | TableCommandResponse.Conflict _ ->
        { StatusCode = StatusCode.Conflict
          ContentType = None
          Headers = new Dictionary<_, _>()
          Body = None }
    | TableList tableNames ->
        let json =
          JsonConvert.SerializeObject
            {| value =
                 tableNames
                 |> Seq.map (fun tableName -> {| TableName = tableName |}) |}

        { StatusCode = StatusCode.Ok
          ContentType = Some ContentType.ApplicationJson
          Headers = [ "Cache-Control", "no-cache" ] |> dict
          Body = Some json }

  let fromCommandResponse (commandResponse: CommandResult) =
    match commandResponse with
    | WriteResponse writeCommandResponse -> fromWriteCommandResponse writeCommandResponse
    | ReadResponse readCommandResponse -> fromReadCommandResponse readCommandResponse
    | TableResponse tableCommandResponse -> fromTableCommandResponse tableCommandResponse
    | BatchResponse batchCommandResponse -> fromBatchCommandResponse batchCommandResponse
    | NotFoundResponse _ ->
        { StatusCode = StatusCode.NotFound
          ContentType = None
          Headers =
            [ "X-Content-Type-Options", "nosniff"
              "Cache-Control", "no-cache" ]
            |> dict
          Body = None }

  let applyToCtx (ctx: HttpContext) (response: Response) =
    task {
      ctx.Response.StatusCode <- int response.StatusCode
      response.Headers
      |> Seq.iter (fun kvp -> ctx.Response.Headers.Add(kvp.Key, StringValues(kvp.Value)))
      if response.ContentType.IsSome
      then ctx.Response.ContentType <- ContentType.toRaw response.ContentType.Value
      if response.Body.IsSome
      then do! ctx.Response.WriteAsync response.Body.Value
    }
