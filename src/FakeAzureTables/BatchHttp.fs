module BatchHttp

open Http
open Domain

let serialize (batchResponse : CommandResult list) =
  ()

let deSerialize (request : Request) : BatchCommand =
  Unchecked.defaultof<BatchCommand>