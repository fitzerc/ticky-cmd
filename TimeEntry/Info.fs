namespace Ticky

open Ticky.TimeEntry

type TimerStatus =
    | Running
    | Stopped

type InfoType =
    { TempPath: string
      RunningEntry: option<RunningTimeEntry>
      TimerStatus: TimerStatus }
