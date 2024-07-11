namespace Ticky

open System
open System.IO
open Ticky.TimeEntry

module FileIo =
    let private tempFileName = ".ticky"
    let private dateFormat = "yyyy-MM-dd"

    //C:\Users\{user}\AppData\Local\Temp\Ticky
    let tempDirPath = Path.Combine(Path.GetTempPath(), @"Ticky\")
    let tempFilePath = tempDirPath + tempFileName

    let private consolidatedFilePath =
        $"{tempDirPath}consolidated-{DateTime.Now.ToString(dateFormat)}.csv"

    let private todaysFileName = $"ticky-{DateTime.Now.ToString(dateFormat)}.csv"

    let private appendToFile directory filename text =
        let path = directory + filename
        File.AppendAllText(path, text)

    let appendToTempFile = appendToFile tempDirPath tempFileName

    let appendToTodaysFile input =
        let filePath = tempDirPath + todaysFileName

        if File.Exists filePath then
            appendToFile tempDirPath todaysFileName input
        else
            appendToFile tempDirPath todaysFileName $"{TimeEntry.getProps}\n"
            appendToFile tempDirPath todaysFileName input

    let writeConsolidatedFile text =
        File.AppendAllLines(consolidatedFilePath, text)

    let deleteTempFile () = File.Delete tempFilePath

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
            Directory.GetFiles(tempDirPath, "*.csv") |> Some
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
