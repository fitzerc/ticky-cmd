namespace Ticky.TickyIo

module ConsoleUtils =
    open System

    let ansiReset = "\u001b[0m"
    let ansiBold = "\u001b[1m"
    let ansiDim = "\u001b[2m"

    let ansiRed = "\u001b[31m"
    let ansiGreen = "\u001b[32m"
    let ansiYellow = "\u001b[33m"
    let ansiBlue = "\u001b[34m"
    let ansiMagenta = "\u001b[35m"
    let ansiCyan = "\u001b[36m"
    let ansiWhite = "\u001b[37m"
    let ansiGray = "\u001b[90m"

    let colorize (color: string) (msg: string) : string = 
        color + msg + ansiReset
    
    let printSuccess (msg: string) : unit = printfn "%s" (colorize ansiGreen msg)
    let printError (msg: string) : unit = printfn "%s" (colorize ansiRed msg)
    let printWarning (msg: string) : unit = printfn "%s" (colorize ansiYellow msg)
    let printInfo (msg: string) : unit = printfn "%s" (colorize ansiCyan msg)
    let printDim (msg: string) : unit = printfn "%s" (colorize ansiGray msg)
    
    let printHeader (msg: string) : unit = 
        printfn ""
        let line = String.replicate (msg.Length) "─"
        printfn "%s%s%s" ansiBold (colorize ansiCyan msg) ansiReset
        printfn "%s" line

    let printKeyValue (key: string) (value: string) : unit =
        printfn "  %s%s: %s" (colorize ansiDim key) ansiReset (colorize ansiWhite value)

    let printSection (title: string) : unit =
        printfn ""
        let prefix = colorize ansiBold (colorize ansiYellow "▸ ")
        let styledTitle = colorize ansiBold title
        printfn "%s%s" prefix styledTitle

    let printTimerStatus (isRunning: bool) : unit =
        if isRunning then
            printfn "%s %s" (colorize ansiGreen "●") (colorize ansiGreen "Timer running")
        else
            printfn "%s %s" (colorize ansiGray "○") (colorize ansiDim "Timer stopped")

    let formatTime (dt: DateTime) : string =
        dt.ToString("HH:mm:ss")

    let formatDate (dt: DateTime) : string =
        dt.ToString("yyyy-MM-dd")

    let printEntryDetails (project: string) (task: string) (tag: string) (startTime: DateTime) : unit =
        printfn ""
        printKeyValue "Project" project
        printKeyValue "Task" task
        printKeyValue "Tag" tag
        printKeyValue "Started" (formatTime startTime)
