open Trik
open Trik.Collections
open Trik.Reactive
open System.Threading

let exit = new EventWaitHandle(false, EventResetMode.AutoReset)

[<EntryPoint>]
let main _ = 
    use model = new Model(PadConfigPort = 9999)

    use lWheel = model.Motors.[M4]
    use rWheel = model.Motors.[M3]
    use pad = model.Pad
    let buttons = model.Buttons

    use powerButtonDisposal = buttons.ToObservable()
                             |> Observable.filter (fun x -> ButtonEventCode.Power = x.Button) 
                             |> Observable.subscribe (fun _ -> lWheel.Stop(); rWheel.Stop(); exit.Set() |> ignore)
    
    use timerStopper = Observable.interval(System.TimeSpan.FromSeconds 2.0)
                       |> Observable.subscribe(fun _ -> lWheel.Stop(); rWheel.Stop())

    use dbtn = pad.Buttons |> Observable.subscribe (fun x ->
        match x with
        | 5 -> printfn "Exiting..."; exit.Set() |> ignore
        | num -> printfn "%A" num)

    use disp = pad.Pads |> Observable.subscribe (fun (_, coord) ->  
        match coord with
        | Some(l, r) -> 
            lWheel.SetPower l
            rWheel.SetPower r 
        | _ -> ()
    )

    printfn "Ready"
    buttons.Start()
    exit.WaitOne() |> ignore

    0