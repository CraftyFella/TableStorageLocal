module HttpRequestParserTests

open Expecto
open System.Text.RegularExpressions
open HttpContext
open Domain

[<Tests>]
let httpRequestParserTests =
  testList
    "HttpRequestParserTests"
    [ test "delete record request from sdk" {

        let http = """POST /devstoreaccount1/$batch HTTP/1.1
Host: localhost.charlesproxy.com:51031
Accept-Charset: UTF-8
MaxDataServiceVersion: 3.0;NetFx
Accept: application/json; odata=minimalmetadata
DataServiceVersion: 3.0;
x-ms-client-request-id: 23ee5deb-2d32-4ccb-84c0-ab7a3509d7a7
User-Agent: Azure-Cosmos-Table/1.0.8 (.NET CLR 3.1.8; Unix 19.6.0.0)
x-ms-version: 2017-07-29
x-ms-date: Mon, 19 Oct 2020 11:03:15 GMT
Authorization: SharedKey devstoreaccount1:SAMVA2Fxmq7FzTwSVJMyNFJ+pER/jGLPqdZhkL+eFJY=
Content-Type: multipart/mixed; boundary=batch_1ca42986-ce81-4222-bfc7-75cf8649d46f
Content-Length: 607

--batch_1ca42986-ce81-4222-bfc7-75cf8649d46f
Content-Type: multipart/mixed; boundary=changeset_2eac933c-14a3-4584-8805-b048af7bccbf

--changeset_2eac933c-14a3-4584-8805-b048af7bccbf
Content-Type: application/http
Content-Transfer-Encoding: binary

DELETE http://localhost.charlesproxy.com:51031/devstoreaccount1/test7(PartitionKey='pk2',RowKey='r2k') HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
DataServiceVersion: 3.0;
If-Match: W/"datetime'2020-10-19T11:03:15Z'"

--changeset_2eac933c-14a3-4584-8805-b048af7bccbf--
--batch_1ca42986-ce81-4222-bfc7-75cf8649d46f--"""

        let result = HttpRequest.parse http
        Expect.isOk result "expected valid http request"

        let batches =
          result
          |> Result.valueOf
          |> HttpRequest.tryExtractBatches

        Expect.isSome batches "unexpected result"
        Expect.equal (batches |> Option.valueOf |> List.length) 1 "unexpected length"
      }

      test "delete record request from storage explorer" {

        let http = """POST /devstoreaccount1/$batch HTTP/1.1
content-type: multipart/mixed; charset="utf-8"; boundary=batch_07ce4b5d1d928c18c57e034fdfc05045
dataserviceversion: 3.0;
maxdataserviceversion: 3.0;NetFx
content-length: 566
x-ms-client-request-id: f2f39270-11f9-11eb-8973-3764f529f0e8
user-agent: Microsoft Azure Storage Explorer, 1.15.0, darwin, Azure-Storage/2.10.3 (NODE-VERSION v12.13.0; Darwin 19.6.0)
x-ms-version: 2018-03-28
x-ms-date: Mon, 19 Oct 2020 10:57:58 GMT
accept: application/atom+xml,application/xml
Accept-Charset: UTF-8
authorization: SharedKey devstoreaccount1:E6GNxoxXRtYJydGeei68KVa6ZLSJ4PwYG9fQVd6y2xs=
host: 127.0.0.1:10002
Connection: keep-alive

--batch_07ce4b5d1d928c18c57e034fdfc05045
content-type: multipart/mixed;charset="utf-8";boundary=changeset_07ce4b5d1d928c18c57e034fdfc05045

--changeset_07ce4b5d1d928c18c57e034fdfc05045
content-type: application/http
content-transfer-encoding: binary

DELETE http://127.0.0.1:10002/devstoreaccount1/test2(PartitionKey=%275%27,RowKey=%275%27) HTTP/1.1
if-match: W/"datetime'2020-10-19T10:49:17Z'"
accept: application/json;odata=minimalmetadata
maxdataserviceversion: 3.0;NetFx


--changeset_07ce4b5d1d928c18c57e034fdfc05045--
--batch_07ce4b5d1d928c18c57e034fdfc05045--"""

        let result = HttpRequest.parse http
        Expect.isOk result "expected valid http request"

        let batches =
          result
          |> Result.valueOf
          |> HttpRequest.tryExtractBatches

        Expect.isSome batches "unexpected result"
        Expect.equal (batches |> Option.valueOf |> List.length) 1 "unexpected length"
      }
      
      test "delete record request from stream stone request" {

        let http = """POST /devstoreaccount1/$batch HTTP/1.1
Host: localhost.charlesproxy.com:54837
Accept-Charset: UTF-8
MaxDataServiceVersion: 3.0;NetFx
Accept: application/json; odata=minimalmetadata
DataServiceVersion: 3.0;
x-ms-client-request-id: a7605906-8dd3-411a-8b2f-bd15c1561d9c
User-Agent: Azure-Cosmos-Table/1.0.8 (.NET CLR 3.1.8; Unix 19.6.0.0)
x-ms-version: 2017-07-29
x-ms-date: Wed, 21 Oct 2020 13:31:27 GMT
Authorization: SharedKey devstoreaccount1:k3etuf4pOS7oyeMQLOatpWDZjjmmreDT2152xk6imus=
Content-Type: multipart/mixed; boundary=batch_14ea6083-4fdb-4d9a-ad2a-81d43ac26f8d
Content-Length: 1390

--batch_14ea6083-4fdb-4d9a-ad2a-81d43ac26f8d
Content-Type: multipart/mixed; boundary=changeset_c8e21d8c-d5a5-49cd-b05a-caf46d2869b1

--changeset_c8e21d8c-d5a5-49cd-b05a-caf46d2869b1
Content-Type: application/http
Content-Transfer-Encoding: binary

MERGE http://localhost.charlesproxy.com:54837/devstoreaccount1/table(PartitionKey='Pools-69d604edb4e14696952bca76d098c829',RowKey='SS-HEAD') HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
DataServiceVersion: 3.0;
If-Match: W/"datetime'2020-10-21T13:31:27Z'"

{"Version":2}
--changeset_c8e21d8c-d5a5-49cd-b05a-caf46d2869b1
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:54837/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-69d604edb4e14696952bca76d098c829","RowKey":"SS-SE-0000000002","Version":2,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"e4bbd3d3-f93e-4533-a894-e9f9a368f885\",\"aggregateId\":\"69d604edb4e14696952bca76d098c829\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T13:31:27.426737+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.2/30\"}}}"}
--changeset_c8e21d8c-d5a5-49cd-b05a-caf46d2869b1--
--batch_14ea6083-4fdb-4d9a-ad2a-81d43ac26f8d--"""

        let result = HttpRequest.parse http
        Expect.isOk result "expected valid http request"

        let batches =
          result
          |> Result.valueOf
          |> HttpRequest.tryExtractBatches

        Expect.isSome batches "unexpected result"
        Expect.equal (batches |> Option.valueOf |> List.length) 2 "unexpected length"
      } ]
