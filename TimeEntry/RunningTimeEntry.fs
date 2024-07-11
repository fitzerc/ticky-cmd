namespace Ticky.TimeEntry

open System
open System.Globalization
open FSharp.Data

type RunningTimeEntry =
    { Project: string
      Task: string
      Tag: string
      StartTime: System.DateTime }

module RunningTimeEntry =
    let dateFormat = "yyyy-MM-dd HH:mm:ss.fff"

    type CsvType =
        CsvProvider<Schema="Project (string), Task (string), Tag (string), StartTime (string)", HasHeaders=false>

    let GetProps: string = "Project,Task,Tag,StartTime"

    let ToCsvRow (timeEntry: RunningTimeEntry) : CsvType.Row =
        CsvType.Row(timeEntry.Project, timeEntry.Task, timeEntry.Tag, timeEntry.StartTime.ToString(dateFormat))

    let ToCsv (timeEntry: RunningTimeEntry) : string =
        let row = ToCsvRow(timeEntry)
        let csvType = new CsvType([ row ])
        csvType.SaveToString()

    let private fromCsvEntries (entries: string array) : option<RunningTimeEntry> =

        let project = entries.[0]
        let task = entries.[1]
        let tag = entries.[2]

        let startTime =
            DateTime.ParseExact(entries.[3], dateFormat, CultureInfo.InvariantCulture)

        Some
            { Project = project
              Task = task
              Tag = tag
              StartTime = startTime }

    let fromCsv (csvString: string) : option<RunningTimeEntry> =
        let entries = csvString.Split(',')

        match entries.Length with
        | 4 -> fromCsvEntries entries
        | _ -> None

    let saveStart appendToTemp runningTimeEntry =
        runningTimeEntry |> ToCsv |> appendToTemp

    let toTimeEntry runningTimeEntry =
        { Project = runningTimeEntry.Project
          Task = runningTimeEntry.Task
          Tag = runningTimeEntry.Tag
          StartTime = runningTimeEntry.StartTime
          EndTime = DateTime.Now
          Elapsed = DateTime.Now - runningTimeEntry.StartTime }
