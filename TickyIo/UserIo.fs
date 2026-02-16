namespace Ticky.TickyIo

open System
open Ticky.TimeEntry

type SaveResponse =
    | Save
    | Cancel

module UserIo =
    let private readLine prompt =
        printfn "%s" prompt
        printf "  > "
        Console.ReadLine() |> Option.ofObj |> Option.defaultValue ""

    let printStr str = printfn "%s\n" str
    let printSymbol symbol = printfn "%A\n" symbol

    let promptAndResult verbiage =
        printfn ""
        printfn "? %s" verbiage
        printf "  (yes/no) [y/n]: "

        match Console.ReadLine() |> Option.ofObj |> Option.defaultValue "" with
        | "y" | "Y" | "yes" | "Yes" -> Save
        | _ -> Cancel

    let promptForEntryDetails () =
        printfn ""
        printfn "Start New Timer"
        printfn "---------------"
        
        let proj = readLine "Project name:"
        let task = readLine "What are you working on?"
        let tag = readLine "Tag (optional):"

        printfn ""

        { Project = proj
          Task = task
          Tag = tag
          StartTime = DateTime.Now }
