module batch

open Expecto
open System
open Host
open Microsoft.Azure.Cosmos.Table
open Domain

(*

POST /$batch HTTP/1.1
Host: podlocaltests.table.core.windows.net
Accept-Charset: UTF-8
MaxDataServiceVersion: 3.0;NetFx
Accept: application/json; odata=minimalmetadata
DataServiceVersion: 3.0;
x-ms-client-request-id: 4344491b-b7a7-406e-ad1a-5499f12ec58a
User-Agent: Azure-Cosmos-Table/1.0.8 (.NET CLR 3.1.3; Unix 19.6.0.0)
x-ms-version: 2017-07-29
x-ms-date: Fri, 09 Oct 2020 17:00:02 GMT
Authorization: SharedKey podlocaltests:4Pn2kwzcPTdAGexCo132DqTTCJPDrqzxGE3ca4wIt9Q=
Content-Type: multipart/mixed; boundary=batch_eaa7e47a-6b1c-4f69-97fa-41fa16ebfcf3
Content-Length: 979

--batch_eaa7e47a-6b1c-4f69-97fa-41fa16ebfcf3
Content-Type: multipart/mixed; boundary=changeset_198b105c-425f-4b3e-8818-8329903fca83

--changeset_198b105c-425f-4b3e-8818-8329903fca83
Content-Type: application/http
Content-Transfer-Encoding: binary

POST https://podlocaltests.table.core.windows.net/dsadasdas() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"pk2","RowKey":"1","StringField":"thing"}
--changeset_198b105c-425f-4b3e-8818-8329903fca83
Content-Type: application/http
Content-Transfer-Encoding: binary

POST https://podlocaltests.table.core.windows.net/dsadasdas() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"pk2","RowKey":"2","StringField":"thing"}
--changeset_198b105c-425f-4b3e-8818-8329903fca83--
--batch_eaa7e47a-6b1c-4f69-97fa-41fa16ebfcf3--


*)

(*

HTTP/1.1 202 Accepted
Cache-Control: no-cache
Transfer-Encoding: chunked
Content-Type: multipart/mixed; boundary=batchresponse_8531abd2-3516-4b0f-8871-5a2d5a829316
Server: Windows-Azure-Table/1.0 Microsoft-HTTPAPI/2.0
x-ms-request-id: d98bfd62-a002-0010-145d-9e9ea3000000
x-ms-version: 2017-07-29
X-Content-Type-Options: nosniff
Date: Fri, 09 Oct 2020 17:00:02 GMT
Connection: keep-alive

--batchresponse_8531abd2-3516-4b0f-8871-5a2d5a829316
Content-Type: multipart/mixed; boundary=changesetresponse_02248134-df38-48c7-9b92-636631c374f2

--changesetresponse_02248134-df38-48c7-9b92-636631c374f2
Content-Type: application/http
Content-Transfer-Encoding: binary

HTTP/1.1 204 No Content
X-Content-Type-Options: nosniff
Cache-Control: no-cache
Preference-Applied: return-no-content
DataServiceVersion: 3.0;
Location: https://podlocaltests.table.core.windows.net/dsadasdas(PartitionKey='pk2',RowKey='1')
DataServiceId: https://podlocaltests.table.core.windows.net/dsadasdas(PartitionKey='pk2',RowKey='1')
ETag: W/"datetime'2020-10-09T17%3A00%3A02.492365Z'"


--changesetresponse_02248134-df38-48c7-9b92-636631c374f2
Content-Type: application/http
Content-Transfer-Encoding: binary

HTTP/1.1 204 No Content
X-Content-Type-Options: nosniff
Cache-Control: no-cache
Preference-Applied: return-no-content
DataServiceVersion: 3.0;
Location: https://podlocaltests.table.core.windows.net/dsadasdas(PartitionKey='pk2',RowKey='2')
DataServiceId: https://podlocaltests.table.core.windows.net/dsadasdas(PartitionKey='pk2',RowKey='2')
ETag: W/"datetime'2020-10-09T17%3A00%3A02.492365Z'"


--changesetresponse_02248134-df38-48c7-9b92-636631c374f2--
--batchresponse_8531abd2-3516-4b0f-8871-5a2d5a829316--
*)


(*

HTTP/1.1 202 Accepted
Date: Thu, 15 Oct 2020 11:48:38 GMT
Content-Type: multipart/mixed; boundary=batchresponse_966bc9886b34472a9764a3915bd603d8
Server: Kestrel
Transfer-Encoding: chunked
Connection: keep-alive


--batchresponse_966bc9886b34472a9764a3915bd603d8
Content-Type: multipart/mixed; boundary=changesetresponse_998d15c5a31c44d088a885c8ce3c9bee
--changesetresponse_998d15c5a31c44d088a885c8ce3c9bee
Content-Type: application/http
Content-Transfer-Encoding: binary
HTTP/1.1 202 Accepted

--changesetresponse_998d15c5a31c44d088a885c8ce3c9bee
Content-Type: application/http
Content-Transfer-Encoding: binary
HTTP/1.1 202 Accepted

--changesetresponse_998d15c5a31c44d088a885c8ce3c9bee--
--batchresponse_966bc9886b34472a9764a3915bd603d8--


*)

[<Tests>]
let batchTests =
  testList
    "batch insert"
    [ test "wibble" {

          let response: BatchCommandResponse = {
            CommandResponses = [ Ack; Ack ]
          }

          let response = BatchHttp.toHttpResponse2 response (Guid.Parse("f325b4d2b9814a6b97e58a7e8959c7da")) (Guid.Parse("f3cd6a4b2d63475f8a4065107d494eef"))

          let expected = """--batchresponse_f325b4d2b9814a6b97e58a7e8959c7da
Content-Type: multipart/mixed; boundary=changesetresponse_f3cd6a4b2d63475f8a4065107d494eef

--changesetresponse_f3cd6a4b2d63475f8a4065107d494eef
Content-Type: application/http
Content-Transfer-Encoding: binary

HTTP/1.1 204 No Content

--changesetresponse_f3cd6a4b2d63475f8a4065107d494eef
Content-Type: application/http
Content-Transfer-Encoding: binary

HTTP/1.1 204 No Content

--changesetresponse_f3cd6a4b2d63475f8a4065107d494eef--
--batchresponse_f325b4d2b9814a6b97e58a7e8959c7da--"""

          Expect.equal response.Body expected ""
      }

      ftest "row doesn't exist is accepted" {
        let table = createFakeTables ()
        let batch = TableBatchOperation()

        createEntityWithString "pk2" "1" "thing"
          |> TableOperation.Insert
          |> batch.Add

        createEntityWithString "pk2" "2" "thing"
          |> TableOperation.Insert
          |> batch.Add

        let actual =
          batch |> table.ExecuteBatch

        Expect.equal (actual |> Seq.length) 2 "unexpected result"
        for batchItem in actual do
          Expect.equal (batchItem.HttpStatusCode) 204 "unexpected result"

      } ]