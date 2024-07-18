open System
open Ticky
open Ticky.TickyIo
open Ticky.TimeEntry

//Main
let runningEntry = FileIo.getRunningEntry

let info =
    { TempPath = FileIo.tempDirPath
      RunningEntry = runningEntry
      TimerStatus =
        match FileIo.getRunningEntry with
        | Some _ -> Running
        | None -> Stopped }

let action =
    Environment.GetCommandLineArgs() |> Array.toList |> CmdLineArgs.parseToAction

match action with
| Start ->
    match info.TimerStatus with
    | Running -> printfn "Timer is already running.\n%A" info
    | Stopped ->
        UserIo.promptForEntryDetails ()
        |> RunningTimeEntry.ToCsv
        |> FileIo.appendToTempFile

        printfn "Timer started"

| Stop ->
    match info.RunningEntry with
    | Some runningTimer ->
        printfn "%A" info

        match UserIo.promptAndResult ("Stop this timer?") with
        | Save ->
            runningTimer
            |> RunningTimeEntry.toTimeEntry
            |> TimeEntry.ToCsv
            |> FileIo.appendToTodaysFile

            FileIo.deleteTempFile ()

        | Cancel -> printfn "No action taken"
    | None -> printfn "Timer is not running"
| Consolidate ->
    match FileIo.getCsvFilesOpt () with
    | Some files ->
        files |> FileIo.consolidateFiles |> Array.toSeq |> FileIo.writeConsolidatedFile

        printfn "Files consolidated"

    | None -> printfn "Nothing to consolidate"
| Info -> printfn "%A" info

exit 100
