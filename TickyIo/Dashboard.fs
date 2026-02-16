namespace Ticky.TickyIo

module Dashboard =
    open System
    open System.IO
    open Ticky.TimeEntry
    open Ticky.TickyIo.ConsoleUtils

    let private dateFormat = "yyyy-MM-dd"
    
    let private todaysFileName () = $"ticky-{DateTime.Now.ToString(dateFormat)}.csv"

    let private formatDuration (ts: TimeSpan) =
        if ts.TotalHours >= 1.0 then
            sprintf "%02d:%02d:%02d" (int ts.TotalHours) ts.Minutes ts.Seconds
        else
            sprintf "%02d:%02d" ts.Minutes ts.Seconds

    let private centerText (text: string) (width: int) =
        let padding = (width - text.Length) / 2
        (String.replicate padding " ") + text + (String.replicate (width - padding - text.Length) " ")

    let private removeAnsi (text: string) =
        text.Replace("\u001b[0m", "")
            .Replace("\u001b[1m", "")
            .Replace("\u001b[2m", "")
            .Replace("\u001b[31m", "")
            .Replace("\u001b[32m", "")
            .Replace("\u001b[33m", "")
            .Replace("\u001b[34m", "")
            .Replace("\u001b[35m", "")
            .Replace("\u001b[36m", "")
            .Replace("\u001b[37m", "")
            .Replace("\u001b[90m", "")

    let private truncate (maxLen: int) (s: string) =
        if s.Length > maxLen then s.Substring(0, maxLen - 2) + ".."
        else s

    let private getOutputFilePath fileName =
        let outputDir = TickyIo.Config.getOutputDirectory()
        Path.Combine(outputDir, fileName)

    let private getTodaysEntries () =
        let filePath = getOutputFilePath (todaysFileName())
        if File.Exists filePath then
            try
                let lines = File.ReadAllLines(filePath)
                if lines.Length > 1 then
                    lines 
                    |> Array.skip 1 
                    |> Array.map (fun line -> 
                        let parts = line.Split(',')
                        if parts.Length >= 6 then
                            Some { 
                                Project = parts.[0]
                                Task = parts.[1]
                                Tag = parts.[2]
                                StartTime = DateTime.Parse(parts.[3])
                                EndTime = DateTime.Parse(parts.[4])
                                Elapsed = TimeSpan.Parse(parts.[5])
                            }
                        else None
                    )
                    |> Array.choose id
                    |> Some
                else None
            with _ -> None
        else None

    let private getRecentEntriesList count =
        let entriesOpt = getTodaysEntries()
        match entriesOpt with
        | Some entries ->
            if Array.isEmpty entries then []
            else
                entries
                |> Array.sortByDescending (fun e -> e.StartTime)
                |> Array.truncate count
                |> Array.toList
        | None -> []

    let private renderRecentEntries (entries: TimeEntry list) selectedIdx expandedIdx =
        if List.isEmpty entries then
            [ colorize ansiDim "No recent entries" ]
        else
            entries
            |> List.mapi (fun i e ->
                let duration = formatDuration e.Elapsed
                let icon = if i = expandedIdx then "v " else "> "
                let projectStr = truncate 14 e.Project
                let line = sprintf "%s%s %s" icon (colorize ansiWhite projectStr) (colorize ansiDim duration)
                if i = selectedIdx && i = expandedIdx then
                    let taskLine = sprintf "  %s" (colorize ansiDim (truncate 40 e.Task))
                    [line; taskLine]
                elif i = selectedIdx then
                    [line]
                else
                    [line]
            )
            |> List.concat

    let private printBox (title: string) (lines: string list) =
        let width = 65
        let border = "─" |> String.replicate width
        let innerWidth = width - 2
        
        printfn ""
        printfn "┌%s┐" border
        
        let plainTitle = title
        let centeredPlain = centerText plainTitle innerWidth
        let styledTitle = sprintf "%s%s%s" ansiBold centeredPlain ansiReset
        let styledLen = (removeAnsi styledTitle).Length
        let pad = innerWidth - styledLen
        let leftPad = pad / 2
        let rightPad = pad - leftPad
        printfn "│ %s%s │" (String.replicate leftPad " ") styledTitle
        
        printfn "├%s┤" border
        
        for line in lines do
            let cleanLine = removeAnsi line
            let pad = innerWidth - cleanLine.Length
            if pad > 0 then
                printfn "│ %s%s │" cleanLine (String.replicate pad " ")
            else
                printfn "│ %s │" cleanLine
        
        printfn "└%s┘" border

    let private printTimerStatus isRunning (startTime: DateTime option) =
        let status = if isRunning then (colorize ansiGreen "● RUNNING") else (colorize ansiGray "○ STOPPED")
        let time = if isRunning && startTime.IsSome then 
                       let elapsed = DateTime.Now - startTime.Value
                       sprintf " %s" (colorize ansiCyan (formatDuration elapsed))
                   else ""
        printfn "%s%s" status time

    let private printTodaysSummary entriesOpt =
        match entriesOpt with
        | Some entries ->
            if Array.isEmpty entries then
                [ colorize ansiDim "No entries today" ]
            else
                let totalSeconds = entries |> Array.sumBy (fun e -> e.Elapsed.TotalSeconds)
                let totalTime = TimeSpan.FromSeconds(totalSeconds)
                let entryCount = entries.Length
                let projectCount = entries |> Array.distinctBy (fun e -> e.Project) |> Array.length
                
                [ sprintf "Total: %s" (colorize ansiCyan (formatDuration totalTime))
                  sprintf "Entries: %s" (colorize ansiWhite (string entryCount))
                  sprintf "Projects: %s" (colorize ansiWhite (string projectCount)) ]
        | None ->
            [ colorize ansiDim "No entries today" ]

    let private printRecentEntries count =
        let entries = getRecentEntriesList count
        if List.isEmpty entries then
            [ colorize ansiDim "No recent entries" ]
        else
            entries
            |> List.mapi (fun i e ->
                let duration = formatDuration e.Elapsed
                let icon = "> "
                let projectStr = truncate 30 e.Project
                sprintf "%s%s %s" icon (colorize ansiWhite projectStr) (colorize ansiDim duration)
            )

    let private renderRecentEntriesInteractive (entries: TimeEntry list) selectedIdx panelSelected =
        if List.isEmpty entries then
            [ colorize ansiDim "No recent entries" ]
        else
            entries
            |> List.mapi (fun i e ->
                let duration = formatDuration e.Elapsed
                let isSelected = i = selectedIdx
                let icon = 
                    if not panelSelected then ""
                    elif isSelected then "> "
                    else "  "
                let projectStr = truncate 30 e.Project
                let line = sprintf "  %s%s %s" icon (colorize ansiWhite projectStr) (colorize ansiDim duration)
                if isSelected && panelSelected then
                    let taskLine = sprintf "      %s" (colorize ansiDim (truncate 50 e.Task))
                    [line; taskLine]
                else
                    [line]
            )
            |> List.concat

    let private getRunningEntry () =
        let tempFilePath = getOutputFilePath ".ticky"
        
        let readFirstLineFromFile path =
            try
                if File.Exists path then
                    let lines = File.ReadAllLines path
                    match lines.Length with
                    | 0 -> None
                    | _ -> Some (Array.head lines)
                else None
            with _ ->
                None
        
        let lineOpt = readFirstLineFromFile tempFilePath
        match lineOpt with
        | Some line -> Ticky.TimeEntry.RunningTimeEntry.fromCsv line
        | None -> None

    let private clearConsole () =
        Console.Clear()

    let private printDashboard isRunning startTime summaryLines recentLines helpLines =
        let width = 65
        let headerWidth = width - 2
        let topBorder = "╔" + (String.replicate headerWidth "─") + "╗"
        let botBorder = "╚" + (String.replicate headerWidth "─") + "╝"
        
        printfn ""
        printfn "%s%s" ansiBold (colorize ansiCyan topBorder)
        
        let plainTitle = "TICKY Time Tracker"
        let titleLen = plainTitle.Length
        let titlePadding = headerWidth - titleLen
        let leftPad = titlePadding / 2
        let rightPad = titlePadding - leftPad
        
        let titleLine = 
            ansiBold + "║" + 
            (String.replicate leftPad " ") + 
            (colorize ansiMagenta "TICKY") + " " + 
            (colorize ansiDim "Time Tracker") + 
            (String.replicate rightPad " ") + 
            "║" + ansiReset
        printfn "%s" titleLine
        
        printfn "%s%s" ansiBold (colorize ansiCyan botBorder)
        
        printTimerStatus isRunning startTime
        
        printBox "Today's Summary" summaryLines
        
        printBox "Recent Entries" recentLines
        
        printBox "Commands" helpLines
        
        printfn ""

    let display () =
        let runningEntry = getRunningEntry ()
        let isRunning = runningEntry.IsSome
        let startTime = runningEntry |> Option.map (fun e -> e.StartTime)
        
        let entriesOpt = getTodaysEntries()
        let summaryLines = printTodaysSummary entriesOpt
        
        let recentEntries = getRecentEntriesList 5
        let recentLines = printRecentEntries 5
        
        let helpLines = 
            [ sprintf "%s start" (colorize ansiGreen "ticky")
              sprintf "%s stop" (colorize ansiRed "ticky")
              sprintf "%s info" (colorize ansiBlue "ticky") ]
        
        printDashboard isRunning startTime summaryLines recentLines helpLines

    type Panel = RecentEntries | Commands

    let private renderCommands isRunning selectedIdx panelSelected =
        let cmd0 = if isRunning then "   Stop Timer" else "   Start Timer"
        let cmd1 = "   Info"
        let cmd2 = "   Quit"
        let cmds = [cmd0; cmd1; cmd2]
        cmds
        |> List.mapi (fun i c ->
            let isSelected = i = selectedIdx
            let prefix = 
                if not panelSelected then "    "
                elif isSelected then "> "
                else "  "
            let colored =
                match i with
                | 0 -> if isRunning then (colorize ansiRed c) else (colorize ansiGreen c)
                | 1 -> (colorize ansiBlue c)
                | _ -> (colorize ansiDim c)
            prefix + colored
        )

    let private printDashboardWithPanels isRunning startTime summaryLines recentPanelSelected recentIdx commandsIdx =
        let width = 65
        let headerWidth = width - 2
        let topBorder = "╔" + (String.replicate headerWidth "─") + "╗"
        let botBorder = "╚" + (String.replicate headerWidth "─") + "╝"
        
        printfn ""
        printfn "%s%s" ansiBold (colorize ansiCyan topBorder)
        
        let plainTitle = "TICKY Time Tracker"
        let titleLen = plainTitle.Length
        let titlePadding = headerWidth - titleLen
        let leftPad = titlePadding / 2
        let rightPad = titlePadding - leftPad
        
        let titleLine = 
            ansiBold + "║" + 
            (String.replicate leftPad " ") + 
            (colorize ansiMagenta "TICKY") + " " + 
            (colorize ansiDim "Time Tracker") + 
            (String.replicate rightPad " ") + 
            "║" + ansiReset
        printfn "%s" titleLine
        
        printfn "%s%s" ansiBold (colorize ansiCyan botBorder)
        
        printTimerStatus isRunning startTime
        
        printBox "Today's Summary" summaryLines
        
        let recentEntries = getRecentEntriesList 5
        let recentBoxTitle = "Recent Entries"
        let recentContent = 
            if List.isEmpty recentEntries then
                [ colorize ansiDim "No recent entries" ]
            else
                renderRecentEntriesInteractive recentEntries recentIdx recentPanelSelected
        printBox recentBoxTitle recentContent
        
        let commands = renderCommands isRunning commandsIdx (not recentPanelSelected)
        let cmdBoxTitle = "Commands"
        printBox cmdBoxTitle commands
        
        printfn ""

    let displayInteractive () : Ticky.Action option =
        if not Console.IsInputRedirected then
            let mutable selectedPanel = RecentEntries
            let mutable recentSelectedIdx = 0
            let mutable commandsSelectedIdx = 0
            let mutable running = true
            let mutable result = None
            let mutable lastRefreshTime = DateTime.Now
            
            while running do
                let runningEntry = getRunningEntry ()
                let isRunning = runningEntry.IsSome
                let startTime = runningEntry |> Option.map (fun e -> e.StartTime)
                
                let entriesOpt = getTodaysEntries()
                let summaryLines = printTodaysSummary entriesOpt
                
                let recentEntries = getRecentEntriesList 5
                let recentMaxIdx = max 0 (List.length recentEntries - 1)
                let commandsMaxIdx = 2
                
                if recentSelectedIdx > recentMaxIdx then recentSelectedIdx <- recentMaxIdx
                if commandsSelectedIdx > commandsMaxIdx then commandsSelectedIdx <- commandsMaxIdx
                
                let currentIdx = 
                    match selectedPanel with
                    | RecentEntries -> recentSelectedIdx
                    | Commands -> commandsSelectedIdx
                
                let effectiveMaxIdx = 
                    match selectedPanel with
                    | RecentEntries -> recentMaxIdx
                    | Commands -> commandsMaxIdx
                
                let recentLines = 
                    if List.isEmpty recentEntries then
                        [ colorize ansiDim "No recent entries" ]
                    else
                        renderRecentEntriesInteractive recentEntries recentSelectedIdx (selectedPanel = RecentEntries)
                
                let helpLines = []
                
                clearConsole ()
                printDashboardWithPanels isRunning startTime summaryLines (selectedPanel = RecentEntries) recentSelectedIdx commandsSelectedIdx
                lastRefreshTime <- DateTime.Now
                
                let mutable keyProcessed = false
                while not keyProcessed do
                    if Console.KeyAvailable then
                        let key = Console.ReadKey(true)
                        match key.Key, key.KeyChar with
                        | ConsoleKey.UpArrow, _
                        | _, 'k' ->
                            if currentIdx > 0 then
                                match selectedPanel with
                                | RecentEntries -> recentSelectedIdx <- recentSelectedIdx - 1
                                | Commands -> commandsSelectedIdx <- commandsSelectedIdx - 1
                            keyProcessed <- true
                        | ConsoleKey.DownArrow, _
                        | _, 'j' ->
                            if currentIdx < effectiveMaxIdx then
                                match selectedPanel with
                                | RecentEntries -> recentSelectedIdx <- recentSelectedIdx + 1
                                | Commands -> commandsSelectedIdx <- commandsSelectedIdx + 1
                            keyProcessed <- true
                        | ConsoleKey.Tab, _ ->
                            match selectedPanel with
                            | RecentEntries -> selectedPanel <- Commands
                            | Commands -> selectedPanel <- RecentEntries
                            keyProcessed <- true
                        | ConsoleKey.Enter, _ ->
                            if selectedPanel = Commands then
                                match commandsSelectedIdx with
                                | 0 -> 
                                    running <- false
                                    clearConsole ()
                                    result <- Some (if isRunning then Ticky.Action.Stop else Ticky.Action.Start)
                                | 1 ->
                                    running <- false
                                    clearConsole ()
                                    result <- Some Ticky.Action.Info
                                | 2 ->
                                    running <- false
                                    clearConsole ()
                                    ()
                                | _ -> ()
                            else
                                ()
                            keyProcessed <- true
                        | ConsoleKey.Q, _ ->
                            running <- false
                            clearConsole ()
                            ()
                            keyProcessed <- true
                        | _ -> keyProcessed <- true
                    else
                        if isRunning && (DateTime.Now - lastRefreshTime).TotalSeconds >= 1.0 then
                            keyProcessed <- true
                        else
                            System.Threading.Thread.Sleep(50)
                done
            
            clearConsole ()
            result
        else
            display ()
            None
