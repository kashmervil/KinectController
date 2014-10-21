module Connection

open System
open Microsoft.Kinect
open System.Net.Sockets
open Graphics

let mutable port = window.portBox.Text
let mutable ipAddr = window.IPBox.Text
let mutable sender: IO.Stream = null 

let tryAccessKinect() = try 
                            KinectSensor.KinectSensors.[0]
                        with :? System.ArgumentOutOfRangeException as e -> 
                           printfn "Kinect not found. Check connection and restart"
                           exit 0
                           null

let startKinectApp (sensor: KinectSensor) =
    async {
        sensor.ColorStream.Enable()
        sensor.SkeletonStream.Enable()
        sensor.Start()
    }
    |> Async.RunSynchronously
    window.modeChooser.IsEnabled <- true


let sendSpeed controller compensation (l,r) =
    sendInstructionMessage ""
    let req = sprintf "pad 1 %d %d" l r |> Text.Encoding.UTF8.GetBytes 
    if sender <> null then
        printfn "speed %d %d   TRIK %d" l r controller
        try 
            sender.Write(req, 0, req.Length)
        with e -> compensation()
    else
        compensation()
        sendInstructionMessage "No connection with trik. Check IP, Port and restart"  

let sendKick controller = 
    let req = "button 1\n"B
    if sender <> null then
        printfn "kick TRIK %d" controller
        try 
            sender.Write(req, 0, req.Length)
        with e -> ()

let setTrikConnection() = 
    port <- window.portBox.Text
    ipAddr <- window.IPBox.Text
    sendInstructionMessage "Establishing connection. Please wait ..."
    sender <- try
                postToUI  <| fun() -> 
                    window.instructionMessage.Content <- "Trik connected. Choose the mode and start playing."
                (new TcpClient(ipAddr, Convert.ToInt32(port))).GetStream()
                with :? SocketException as e ->
                postToUI <| fun() -> 
                    window.instructionMessage.Content <- "No connection with trik. Check IP, Port and restart"
                    window.conectionButton.IsChecked <- new Nullable<bool>(false)
                null
    sender <> null
