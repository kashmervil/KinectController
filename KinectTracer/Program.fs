open Trik
open System.Threading

let exit = new EventWaitHandle(false, EventResetMode.AutoReset)

[<EntryPoint>]
let main _ = 
    use model = new Model(PadConfigPort = 9999)

    let lWheel = model.Motor.["M1"]
    let rWheel = model.Motor.["M3"]
    let pad = model.Pad

    use dbtn = pad.Buttons.Subscribe (fun x ->
        match x with
        | 5 -> printfn "Exiting..."; exit.Set() |> ignore
        | num -> printfn "%A" num)

    use disp = pad.Pads.Subscribe (fun (_,coord) ->  
        match coord with
        | Some(l, r) -> 
            //printfn "%d %d" l r
            lWheel.SetPower l
            rWheel.SetPower r 
        | _ -> () 
    )

    printfn "Ready"
    exit.WaitOne() |> ignore
    printfn "Exiting (after wait)"
    Thread.Sleep 1000
    0