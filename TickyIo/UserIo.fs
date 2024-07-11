namespace Ticky.TickyIo

open System
open Ticky.TimeEntry

type SaveResponse =
    | Save
    | Cancel

module UserIo =
    let printStr str = printfn "%s\n" str
    let printSymbol symbol = printfn "%A\n" symbol

    let promptAndResult verbiage =
        printfn $"{verbiage} y/n"

        match Console.ReadLine() with
        | "y" -> Save
        | _ -> Cancel

    let promptForEntryDetails () =
        printfn "Project:"
        let proj = Console.ReadLine()

        printfn "Task:"
        let task = Console.ReadLine()

        printfn "Tag:"
        let tag = Console.ReadLine()

        { Project = proj
          Task = task
          Tag = tag
          StartTime = DateTime.Now }
