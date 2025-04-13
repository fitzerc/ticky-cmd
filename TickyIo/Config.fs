namespace TickyIo

open System.IO
open System.Text.Json

module Config =
    let private configFilePath = "appsettings.json"

    let getOutputDirectory () =
        try
            let json = File.ReadAllText(configFilePath)
            let config = JsonSerializer.Deserialize<Map<string, string>>(json)
            match config.TryFind "OutputDirectory" with
            | Some dir -> dir
            | None -> "/home/chris/Code/ticky-cmd/output" // Default value
        with
        | _ -> "/home/chris/Code/ticky-cmd/output" // Default value in case of error