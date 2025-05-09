namespace Ticky

open System
open System.IO
open Ticky.TimeEntry
open TickyIo.Config

module FileIo =
    let private tempFileName = ".ticky"
    let private dateFormat = "yyyy-MM-dd"

    let tempDirPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Ticky\")

    let tempFilePath = Path.Combine(getOutputDirectory(), tempFileName)

    let private consolidatedFilePath =
        Path.Combine(getOutputDirectory(), $"consolidated-{DateTime.Now.ToString(dateFormat)}.csv")

    let private todaysFileName = $"ticky-{DateTime.Now.ToString(dateFormat)}.csv"

    let getOutputFilePath fileName =
        let outputDir = TickyIo.Config.getOutputDirectory()
        Path.Combine(outputDir, fileName)

    let private appendToFile directory filename text =
        let path = Path.Combine(directory, filename)
        File.AppendAllText(path, text)

    let appendToTempFile = appendToFile (getOutputDirectory()) tempFileName

    let appendToTodaysFile input =
        let filePath = getOutputFilePath todaysFileName

        if File.Exists filePath then
            appendToFile (getOutputDirectory()) todaysFileName input
        else
            appendToFile (getOutputDirectory()) todaysFileName $"{TimeEntry.getProps}\n"
            appendToFile (getOutputDirectory()) todaysFileName input

    let writeConsolidatedFile text =
        let filePath = getOutputFilePath $"consolidated-{DateTime.Now.ToString(dateFormat)}.csv"
        File.AppendAllLines(filePath, text)

    let deleteTempFile () = 
        let filePath = getOutputFilePath tempFileName
        File.Delete filePath

    let private readFromFile (path) =
        try
            let lines = File.ReadAllLines path

            match lines.Length with
            | 0 -> None
            | _ -> Some lines
        with _ ->
            None

    let readFirstLineFromFile path =
        let linesOption = readFromFile path

        match linesOption with
        | Some lines -> Array.head lines |> Some
        | None -> None

    let getRunningEntry =
        let lineOpt = readFirstLineFromFile tempFilePath

        match lineOpt with
        | Some line -> RunningTimeEntry.fromCsv line
        | None -> None

    let isTimerRunning runningTimerOpt =
        match runningTimerOpt with
        | Some _ -> true
        | None -> false

    let getCsvFilesOpt () =
        try
            Directory.GetFiles(getOutputDirectory(), "*.csv") |> Some
        with _ ->
            None

    let rec private consolidate (files: string array, newFile: string array) =
        if files.Length = 0 then
            newFile
        else
            let file = File.ReadAllLines(Array.head (files)) |> Array.tail //skip header row
            let tailFiles = Array.tail files

            let updatedFile = Array.append newFile file
            consolidate (tailFiles, updatedFile)

    let private summarizeByProject (entries: string array) =
        entries
        |> Array.filter (fun entry -> not (entry.StartsWith("Project"))) // Exclude header row
        |> Array.map (fun entry -> entry.Split(','))
        |> Array.groupBy (fun fields -> fields.[0]) // Group by Project (first column)
        |> Array.map (fun (project, records) ->
            let totalElapsed =
                records
                |> Array.sumBy (fun fields ->
                    let elapsed = System.TimeSpan.Parse(fields.[5]) // Elapsed is the 6th column
                    elapsed.TotalSeconds
                )
            let totalElapsedTime = System.TimeSpan.FromSeconds(totalElapsed).ToString("hh\:mm\:ss")
            let entryCount = records.Length
            $"{project},{totalElapsedTime},{entryCount}")

    let private summarizeByTag (entries: string array) =
        entries
        |> Array.filter (fun entry -> not (entry.StartsWith("Project"))) // Exclude header row
        |> Array.map (fun entry -> entry.Split(','))
        |> Array.groupBy (fun fields -> fields.[2]) // Group by Tag (third column)
        |> Array.map (fun (tag, records) ->
            let totalElapsed =
                records
                |> Array.sumBy (fun fields ->
                    let elapsed = System.TimeSpan.Parse(fields.[5]) // Elapsed is the 6th column
                    elapsed.TotalSeconds
                )
            let totalElapsedTime = System.TimeSpan.FromSeconds(totalElapsed).ToString("hh\:mm\:ss")
            let entryCount = records.Length
            $"{tag},{totalElapsedTime},{entryCount}")

    let consolidateFiles files =
        let header = [| TimeEntry.getProps |]
        let consolidatedEntries = consolidate (files, header)
        let groupedSummary = summarizeByProject consolidatedEntries |> Array.append [| ""; "Project Summary" |]
        let tagSummary = summarizeByTag consolidatedEntries |> Array.append [| ""; "Tag Summary" |]
        Array.append (Array.append consolidatedEntries groupedSummary) tagSummary

    let consolidateFilesForDay (files: string array) =
        let header = [| TimeEntry.getProps |]
        let todayFileName = $"ticky-{DateTime.Now.ToString(dateFormat)}.csv"
        let todayFile = files |> Array.tryFind (fun file -> file.EndsWith(todayFileName))

        match todayFile with
        | Some file ->
            let consolidatedEntries = consolidate ([| file |], header)
            let groupedSummary = summarizeByProject consolidatedEntries |> Array.append [| ""; "Project Summary" |]
            let tagSummary = summarizeByTag consolidatedEntries |> Array.append [| ""; "Tag Summary" |]
            let dailySummary = Array.append groupedSummary tagSummary

            let dailySummaryFileName = $"daily-summary-{DateTime.Now.ToString(dateFormat)}.csv"
            let dailySummaryFilePath = getOutputFilePath dailySummaryFileName

            File.WriteAllLines(dailySummaryFilePath, dailySummary)
            dailySummary
        | None ->
            [| "No entries found for today." |]
