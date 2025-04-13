open System
open System.IO
open System.Text.Json
open System.Collections.Generic
open Ticky
open Ticky.TickyIo
open Ticky.TimeEntry

// Load configuration from appsettings.json
let loadConfig () : Dictionary<string, string> =
    let configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")
    let json = File.ReadAllText(configPath)
    JsonSerializer.Deserialize<Dictionary<string, string>>(json)

let config = loadConfig ()
let tempPath =
    match config.TryGetValue("OutputDirectory") with
    | true, path -> path
    | _ -> "Default/Temp/Path"

// Call the displayStatusMessage function to show status updates
let displayStatusMessage status =
    match status with
    | "Success" -> printfn "✅ Operation completed successfully!"
    | "Error" -> printfn "❌ An error occurred during the operation."
    | "InProgress" -> printfn "⏳ Operation is in progress..."
    | _ -> printfn "ℹ️ Status: %s" status

//Main
let runningEntry = FileIo.getRunningEntry

let info =
    { TempPath = tempPath
      RunningEntry = runningEntry
      TimerStatus =
        match runningEntry with
        | Some _ -> Running
        | None -> Stopped }

let action =
    Environment.GetCommandLineArgs() |> Array.toList |> CmdLineArgs.parseToAction

match action with
| Start ->
    match info.TimerStatus with
    | Running -> 
        printfn "Timer is already running.\n%A" info
        displayStatusMessage "Error"
    | Stopped ->
        UserIo.promptForEntryDetails ()
        |> RunningTimeEntry.ToCsv
        |> FileIo.appendToTempFile

        printfn "Timer started"
        displayStatusMessage "Success"

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
            displayStatusMessage "Success"

        | Cancel -> 
            printfn "No action taken"
            displayStatusMessage "InProgress"
    | None -> 
        printfn "Timer is not running"
        displayStatusMessage "Error"
| Consolidate ->
    match FileIo.getCsvFilesOpt () with
    | Some files ->
        files |> FileIo.consolidateFiles |> Array.toSeq |> FileIo.writeConsolidatedFile

        printfn "Files consolidated"
        displayStatusMessage "Success"

    | None -> 
        printfn "Nothing to consolidate"
        displayStatusMessage "Error"
| Info -> 
    printfn "%A" info
    displayStatusMessage "InProgress"

exit 100
