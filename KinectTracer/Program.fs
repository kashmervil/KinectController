open Trik
open Trik.Collections
open System.Threading

let exit = new EventWaitHandle(false, EventResetMode.AutoReset)

[<EntryPoint>]
let main _ = 
    use model = new Model(PadConfigPort = 9999)

    use lWheel = model.Motors.[M1]
    use rWheel = model.Motors.[M2]
    use pad = model.Pad
    use buttons = model.Buttons

    use downButtonDisposal = buttons.ToObservable()
                             |> Observable.filter (fun x -> ButtonEventCode.Down = x.Button) 
                             |> Observable.subscribe (fun _ -> lWheel.Stop(); rWheel.Stop(); exit.Set() |> ignore)

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