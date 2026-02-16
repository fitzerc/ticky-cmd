open System
open System.IO
open System.Text.Json
open System.Collections.Generic
open Ticky
open Ticky.TickyIo
open Ticky.TickyIo.ConsoleUtils
open Ticky.TickyIo.Dashboard
open Ticky.TimeEntry

type TickyAction = Ticky.Action

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

//Main
let getRunningEntry () = FileIo.getRunningEntry ()

let getInfo () =
    let runningEntry = getRunningEntry ()
    { TempPath = tempPath
      RunningEntry = runningEntry
      TimerStatus =
        match runningEntry with
        | Some _ -> Running
        | None -> Stopped }

let doStart () =
    let info = getInfo ()
    match info.TimerStatus with
    | Running -> 
        printfn ""
        printError "Timer is already running!"
        printfn ""
        printKeyValue "Started" (info.RunningEntry |> Option.map (fun e -> ConsoleUtils.formatTime e.StartTime) |> Option.defaultValue "")
        printKeyValue "Project" (info.RunningEntry |> Option.map (fun e -> e.Project) |> Option.defaultValue "")
        printKeyValue "Task" (info.RunningEntry |> Option.map (fun e -> e.Task) |> Option.defaultValue "")
    | Stopped ->
        let entry = UserIo.promptForEntryDetails ()
        entry
        |> RunningTimeEntry.ToCsv
        |> FileIo.writeToTempFile

        printfn ""
        printSuccess "✓ Timer started"
        printfn ""
        ConsoleUtils.printEntryDetails entry.Project entry.Task entry.Tag entry.StartTime

let doStop (prompt: bool) =
    let runningEntry = getRunningEntry ()
    match runningEntry with
    | Some runningTimer ->
        printfn ""
        printSection "Stop Timer"
        ConsoleUtils.printEntryDetails runningTimer.Project runningTimer.Task runningTimer.Tag runningTimer.StartTime
        
        if prompt then
            match UserIo.promptAndResult "Save this time entry?" with
            | Save ->
                runningTimer
                |> RunningTimeEntry.toTimeEntry
                |> TimeEntry.ToCsv
                |> FileIo.appendToTodaysFile

                FileIo.deleteTempFile ()
                printfn ""
                printSuccess "✓ Time entry saved"
                printfn ""
                printDim "Saved to today's log"

            | Cancel -> 
                printfn ""
                printDim "Timer continues running..."
        else
            runningTimer
            |> RunningTimeEntry.toTimeEntry
            |> TimeEntry.ToCsv
            |> FileIo.appendToTodaysFile

            FileIo.deleteTempFile ()
            printfn ""
            printSuccess "✓ Time entry saved"
            printfn ""
            printDim "Saved to today's log"
    | None -> 
        printfn ""
        printWarning "No timer is currently running"
        printfn "Use 'ticky start' to begin tracking time"

let doInfo () =
    let info = getInfo ()
    printfn ""
    printHeader "Timer Status"
    printTimerStatus (info.TimerStatus = Running)
    
    let runningEntry = getRunningEntry ()
    match runningEntry with
    | Some entry ->
        printfn ""
        ConsoleUtils.printEntryDetails entry.Project entry.Task entry.Tag entry.StartTime
    | None ->
        printfn ""
        printDim "No active time entry"

let doConsolidate () =
    let args = Environment.GetCommandLineArgs()
    if args |> Array.contains "-d" then
        match FileIo.getCsvFilesOpt () with
        | Some files ->
            files |> FileIo.consolidateFilesForDay |> ignore

            printfn ""
            printSuccess "✓ Daily summary generated"
            printfn ""
            printDim "Saved to daily-summary file"

        | None -> 
            printfn ""
            printWarning "No entries found for today"
    else
        match FileIo.getCsvFilesOpt () with
        | Some files ->
            files |> FileIo.consolidateFiles |> Array.toSeq |> FileIo.writeConsolidatedFile

            printfn ""
            printSuccess "✓ Files consolidated"
            printfn ""
            printDim "Saved to consolidated file"

        | None -> 
            printfn ""
            printWarning "No CSV files found to consolidate"

let rec runDashboard () =
    match Dashboard.displayInteractive () with
    | Some act ->
        match act with
        | TickyAction.Start -> 
            doStart ()
            runDashboard ()
        | TickyAction.Stop -> 
            doStop false
            runDashboard ()
        | TickyAction.Info -> 
            doInfo ()
            runDashboard ()
        | TickyAction.Consolidate -> 
            doConsolidate ()
            runDashboard ()
        | TickyAction.Dashboard -> runDashboard ()
    | None -> ()

let action =
    Environment.GetCommandLineArgs() |> Array.toList |> CmdLineArgs.parseToAction

match action with
| Start -> doStart ()
| Stop -> doStop true
| Info -> doInfo ()
| Consolidate -> doConsolidate ()
| Dashboard ->
    runDashboard ()

exit 0
