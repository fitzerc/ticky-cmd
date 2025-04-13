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

            let updatedFile = Array.append file newFile
            consolidate (tailFiles, updatedFile)

    let consolidateFiles files =
        consolidate (files, [| TimeEntry.getProps |])
