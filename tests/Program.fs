module Program

open Expecto

[<EntryPoint>]
let main args =
  let config =
    { defaultConfig with
        verbosity = Logging.Debug }

  runTestsInAssembly config args
