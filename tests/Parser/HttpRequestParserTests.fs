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
      
      test "merge and insert request from stream stone request" {

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
      } 
      
      test "insert 100 records request from stream stone request" {

        let http = """POST /devstoreaccount1/$batch HTTP/1.1
Host: localhost.charlesproxy.com:60935
Accept-Charset: UTF-8
MaxDataServiceVersion: 3.0;NetFx
Accept: application/json; odata=minimalmetadata
DataServiceVersion: 3.0;
x-ms-client-request-id: 97589ecf-31dc-48c9-9860-f2aa467bb021
User-Agent: Azure-Cosmos-Table/1.0.8 (.NET CLR 3.1.8; Unix 19.6.0.0)
x-ms-version: 2017-07-29
x-ms-date: Wed, 21 Oct 2020 14:31:30 GMT
Authorization: SharedKey devstoreaccount1:3+sqZRYI3KRyHGPhYqXINCRIpggOKjto2txhusq42as=
Content-Type: multipart/mixed; boundary=batch_4b0cb89b-eb1c-4872-90cc-d9f184f53667
Content-Length: 73094

--batch_4b0cb89b-eb1c-4872-90cc-d9f184f53667
Content-Type: multipart/mixed; boundary=changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013

--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-HEAD","Version":99}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000001","Version":1,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"81d16161-dab5-4e0e-9dd3-f190ff3a14bb\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.334444+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000002","Version":2,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"63094fc5-52dc-4ddc-871d-7112bb3d03ed\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.422693+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000003","Version":3,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"f9539d7b-efd4-475c-89c2-ddecfa06949b\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.423323+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000004","Version":4,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"945909cd-7267-4465-b751-0a2ee1d12743\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.423662+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000005","Version":5,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"e5533bcf-71d1-4a58-b4dc-75c9ecb0bc07\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.424066+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000006","Version":6,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"21584a1d-5010-4a60-8b7a-6fec7b677b29\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.424545+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000007","Version":7,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"90b769fe-139b-4f51-bb43-4fdd6dceb977\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.424888+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000008","Version":8,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"7760cb8b-9cb2-4212-9a2e-bbce60732b28\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.425216+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000009","Version":9,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"aa9ad609-526f-4c37-9666-9eb6f54f337c\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.425667+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000010","Version":10,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"ea3b266e-3d22-4ddd-b0d5-1edc8b630b74\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.42602+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000011","Version":11,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"426c8206-6d2b-4e0d-9ff1-4fc62b7ae4a7\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.42639+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000012","Version":12,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"7ff9797e-eca5-4259-a845-2a3a1de743cb\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.426822+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000013","Version":13,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"4e0493e9-676d-4fba-8be5-f42773d4cb30\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.427161+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000014","Version":14,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"a74bee82-de48-4e1f-9009-f89bf9fc23c8\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.427466+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000015","Version":15,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"6a3a3aed-0d2d-4250-a1f9-bd296bdcf624\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.427763+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000016","Version":16,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"68b22dc2-4018-41ac-870b-8439a27fe449\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.428427+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000017","Version":17,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"85874aed-5cda-42f6-9dc3-78bb61ee4a56\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.429072+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000018","Version":18,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"3984cefa-bc53-48d8-a217-4739616a8ac4\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.42993+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000019","Version":19,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"471ccd20-9c32-41f1-97fa-188b03de9a40\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.430465+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000020","Version":20,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"e102fefd-414f-49d1-9ee8-a60bf0ba1360\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.431106+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000021","Version":21,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"17a5b302-07bb-4b3c-9efb-b0f938629edb\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.431579+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000022","Version":22,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"c6988bcd-25fe-454c-ab47-a6869053fa89\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.432043+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000023","Version":23,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"825937aa-f430-45a7-acc9-484560f23cb2\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.432365+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000024","Version":24,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"81fad127-1c96-45a3-b864-941c10fca654\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.432688+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000025","Version":25,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"16e945c5-9216-467d-be05-db529957c1a9\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.43867+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000026","Version":26,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"384aac54-60de-4ff5-a06b-5cf9fea64aca\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.439537+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000027","Version":27,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"6ca8afe0-1df2-4096-884b-80c5d88c4048\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.439828+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000028","Version":28,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"ba85787c-383b-4371-9506-bb285f73c0f4\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.440092+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000029","Version":29,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"4b8deaf1-8922-4110-8b90-803fe111ec4f\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.44035+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000030","Version":30,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"a2fd6eb9-cfa4-4d59-b30c-596a7e579fc8\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.440627+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000031","Version":31,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"df74ce11-bfc9-43cf-ac4a-2b559799d58f\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.440889+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000032","Version":32,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"bea9d7ff-0d8e-4228-bc6e-207cf6d818a1\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.441225+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000033","Version":33,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"53ced0cf-3292-4f0d-a80b-1445fa2b1620\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.441502+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000034","Version":34,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"74713aa2-c320-4c6d-9910-c45ef8465a5c\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.441876+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000035","Version":35,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"d57285f3-9968-4b7b-9c34-8a97e8a7d932\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.442226+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000036","Version":36,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"0b00f0e5-db31-4d45-91c8-c3e6bdda48df\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.442494+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000037","Version":37,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"5b0ebd07-90ce-4651-83af-2f1e29af7bca\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.442756+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000038","Version":38,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"fdf14b30-5ba2-40a1-a63e-b41d243e14da\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.443018+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000039","Version":39,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"ff176fd4-c946-4b1f-904c-f489f95f0f03\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.443335+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000040","Version":40,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"205bd471-dcab-4d2e-90b9-4c11efddf12e\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.443617+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000041","Version":41,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"c8e5250f-1571-4b4f-a77f-adc876a19a03\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.443884+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000042","Version":42,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"aa91d84d-92f0-4689-b0f6-599b8356bfcc\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.444154+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000043","Version":43,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"7bf39c02-934e-4507-a48d-cea8837147e8\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.444591+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000044","Version":44,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"cc8e59f9-d1fe-47d8-bea1-844952be87f9\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.444877+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000045","Version":45,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"64f848d5-6cbd-43aa-9545-d9c6f6d9250f\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.445159+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000046","Version":46,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"4d5e9ac8-cc1d-4672-921e-cc268c22837a\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.44542+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000047","Version":47,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"d3ab4dfa-da94-4f31-8c03-8d6449be3607\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.445696+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000048","Version":48,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"261d3d68-f1b1-4dc2-82ab-2325df634285\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.445958+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000049","Version":49,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"0cb7e606-4466-458b-87b8-6db781994475\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.446375+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000050","Version":50,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"6350734f-3c6b-4e7d-ad77-578153e560e5\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.446655+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000051","Version":51,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"f28c07e8-5d39-4fe7-896a-a07e1eb6704b\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.447182+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000052","Version":52,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"110fcb3c-2207-4318-af1a-f4b79d5997f7\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.447559+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000053","Version":53,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"0202c4f9-3c4d-4d47-8e3d-3dfc1c167927\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.447991+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000054","Version":54,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"22d77b3e-f913-4914-b859-e43b366da4bf\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.448415+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000055","Version":55,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"b412d76a-3a8b-483c-afc0-eb52113af554\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.448717+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000056","Version":56,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"9c205676-863f-415c-b77b-35a394a8edd0\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.449059+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000057","Version":57,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"9c30ff08-5657-43e4-8896-0c4cd58ec46d\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.449355+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000058","Version":58,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"9f88d7c8-7f6d-41e0-aaee-5f46f22ad42a\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.449672+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000059","Version":59,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"78178ca1-0f60-4106-979e-0eb46dcbdca7\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.450363+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000060","Version":60,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"0bb15ee1-375a-461d-86b1-05dc49b70648\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.450681+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000061","Version":61,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"3fef04ff-4e20-49d9-8191-03bb2e9903b8\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.451172+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000062","Version":62,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"200287cb-135e-4115-9893-f2edebe38988\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.451786+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000063","Version":63,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"8fbe126d-bd43-445d-8e53-6e192bec87d3\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.452496+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000064","Version":64,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"36058988-b905-4601-a8a4-d81bd0148351\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.452873+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000065","Version":65,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"0355c20b-f357-49c7-96ba-e76d7abb02b2\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.453157+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000066","Version":66,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"3f15c239-4a26-45d5-b614-b1acbbfd63c9\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.453458+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000067","Version":67,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"e9fe9d0a-635c-4490-9a9f-d5426cb43872\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.45379+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000068","Version":68,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"9931cdc5-9cb0-460f-8326-93669c737e43\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.454214+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000069","Version":69,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"29018313-f56c-4fbe-85a3-096d270ed102\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.454559+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000070","Version":70,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"e6f0d8b9-72d2-4cee-b48c-6fc80ea9cfcb\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.45483+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000071","Version":71,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"5902c338-fe92-42fd-a5a3-dbc6f1de4727\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.455137+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000072","Version":72,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"51156cd2-7dcd-4827-8535-8339e9311a63\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.455427+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000073","Version":73,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"7f2b946f-382a-4390-8ada-0c4e112238d1\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.455765+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000074","Version":74,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"fa85ec62-5b17-41fd-b8d9-9eb3bb2400cd\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.456182+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000075","Version":75,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"1be564a1-b64f-434a-b16c-475174df17ba\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.456481+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000076","Version":76,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"21c6ae19-a79f-4ff7-80cf-b103ffb9d3a6\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.456758+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000077","Version":77,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"bc80e82e-f136-43c5-87fe-feebc167ac0d\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.45703+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000078","Version":78,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"98a9801a-f7a9-4930-b8a6-f6afc7bac733\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.457295+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000079","Version":79,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"24ad1688-b6cf-4e81-9d87-2e5eca342ebc\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.457618+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000080","Version":80,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"32df5676-006c-4237-9625-4f60cb1027a2\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.457864+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000081","Version":81,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"eaccd972-1b5b-45c7-9f8d-c0bbe7ff6a28\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.458104+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000082","Version":82,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"6f91c3f1-626d-401c-a463-69c0c139372c\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.458404+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000083","Version":83,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"0e680011-aa32-45ce-a256-39271f4039c8\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.458688+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000084","Version":84,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"7be36ae4-525f-48c1-bdaa-0f79c694166d\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.45899+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000085","Version":85,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"9cc9cdbb-206b-4be4-9279-29d00b73f891\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.459263+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000086","Version":86,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"93659929-6931-4b8c-afe9-0e67880b8a9b\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.459527+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000087","Version":87,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"cd521021-7db2-4d14-8552-c0bdb5c1d69d\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.459798+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000088","Version":88,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"48b1fccc-1135-4e1e-be7d-8f8db459c5aa\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.460061+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000089","Version":89,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"e6b50647-b1eb-4375-8968-57aeea908829\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.460437+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000090","Version":90,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"c5af801b-d4b6-4a4d-9a02-d9602b1007b6\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.460836+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000091","Version":91,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"8fe90708-fd81-4576-9bba-4dc181082b39\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.461116+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000092","Version":92,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"cf139ed1-7b18-47f6-9981-647182e720ad\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.461375+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000093","Version":93,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"9fc93190-9cb4-4935-8830-2e5e776abb2c\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.461656+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000094","Version":94,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"9bfade20-f377-4bb3-aed2-cffb1254fe7d\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.461921+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000095","Version":95,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"6802d0e8-a11c-4025-a51f-98334291d888\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.462434+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000096","Version":96,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"a5cd2d40-441b-4098-ac14-049583468860\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.463262+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000097","Version":97,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"a50eafd8-4fc3-4ec8-96ae-8355e23bf2c4\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.463803+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000098","Version":98,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"a5b218e7-5f0d-4fc3-b327-fa6a7aa5b738\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.464107+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013
Content-Type: application/http
Content-Transfer-Encoding: binary

POST http://localhost.charlesproxy.com:60935/devstoreaccount1/table() HTTP/1.1
Accept: application/json;odata=minimalmetadata
Content-Type: application/json
Prefer: return-no-content
DataServiceVersion: 3.0;

{"PartitionKey":"Pools-eab779e9231a4688b798fdcd9e45cbb6","RowKey":"SS-SE-0000000099","Version":99,"Data":"{\"metadata\":{\"tag\":\"CidrBlockAdded\",\"id\":\"c63614cf-4c4d-4f29-8451-66837bbd0c55\",\"aggregateId\":\"eab779e9231a4688b798fdcd9e45cbb6\",\"stream\":\"Pools\",\"timestamp\":\"2020-10-21T14:31:30.464391+00:00\"},\"data\":{\"cidrBlockAdded\":{\"poolName\":\"name\",\"cidrBlock\":\"1.1.1.0/30\"}}}"}
--changeset_d57d2b29-03e3-418b-9c4a-0002ba68e013--
--batch_4b0cb89b-eb1c-4872-90cc-d9f184f53667--"""

        let result = HttpRequest.parse http
        Expect.isOk result "expected valid http request"

        let batches =
          result
          |> Result.valueOf
          |> HttpRequest.tryExtractBatches

        Expect.isSome batches "unexpected result"
        Expect.equal (batches |> Option.valueOf |> List.length) 100 "unexpected length"
      } ]
