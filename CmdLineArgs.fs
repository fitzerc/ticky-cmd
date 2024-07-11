namespace Ticky

module CmdLineArgs =
    let parseToAction allArgs =
        let args = List.tail allArgs

        match args with
        | "start" :: xs -> Start
        | "info" :: xs -> Info
        | "stop" :: xs -> Stop
        | "consolidate" :: xs -> Consolidate
        | _ -> Info
