namespace Ticky.TimeEntry

type TimeEntry =
    { Project: string
      Task: string
      Tag: string
      StartTime: System.DateTime
      EndTime: System.DateTime
      Elapsed: System.TimeSpan }

module TimeEntry =
    open FSharp.Data

    let dateFormat = "yyyy-MM-dd HH:mm:ss.fff"

    type CsvType =
        CsvProvider<
            Schema="Project (string), Task (string), Tag (string), StartTime (string), EndTime (string), Elapsed (string)",
            HasHeaders=false
         >

    let getProps: string = "Project,Task,Tag,StartTime,EndTime,Elapsed"

    let private toCsvRow (timeEntry: TimeEntry) : CsvType.Row =
        CsvType.Row(
            timeEntry.Project,
            timeEntry.Task,
            timeEntry.Tag,
            timeEntry.StartTime.ToString(dateFormat),
            timeEntry.StartTime.ToString(dateFormat),
            timeEntry.Elapsed.ToString(@"hh\:mm\:ss")
        )

    let ToCsv (timeEntry: TimeEntry) : string =
        let row = toCsvRow (timeEntry)
        let csvType = new CsvType([ row ])
        csvType.SaveToString()
