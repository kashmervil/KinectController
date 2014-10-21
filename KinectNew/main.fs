open System
open System.Windows
open System.Reactive.Linq
open SkeletonProcessing
open Connection
open Graphics
open Microsoft.FSharp.Collections
                                    
[<EntryPoint;STAThread>]
let main _ =
    
    let kinect = tryAccessKinect()
    window.conectionButton.Checked.Subscribe(fun _ -> if setTrikConnection() then startKinectApp kinect) |> ignore
    
    let skeletonFrame = kinect.SkeletonFrameReady.Select ExtractTrackedSkeletons
    let skeltonsId = skeletonFrame |> Observable.map (Array.map trackingId) |> Observable.DistinctUntilChanged
    
    use printer = skeltonsId.Subscribe(fun ts -> mvvm.TrackedSkeletons <- ts)
    use videoDisp = kinect.ColorFrameReady.Subscribe ColorFrameReady
    
    let activeSkeletons = new Collections.Generic.List<int * IDisposable>(5)
    let safeAddSub a = lock activeSkeletons <| fun () -> activeSkeletons.Add a            
    let safeRemove id = lock activeSkeletons <| fun () -> 
            let i = activeSkeletons.FindIndex( fun (i,_) -> i = id)
            if i >= 0 then let _,d = activeSkeletons.[i]
                           activeSkeletons.RemoveAt(i)
                           d.Dispose()

    let joinPlayer n = 
        let robot = 0
        let points = skeletonFrame 
                     |> Observable.choose (Array.tryFind (fun x -> trackingId x = n))
                     |> Observable.choose getPoints
            
        let d1 = points 
                 |> ObservableHelpers.TakeDerivative //  
                 |> ObservableHelpers.Buffer 10 1    //   Making flapping routine 
                 |> Observable.map tupleAverage      //
                 |> Observable.map flappingScale     //
                 |> Observable.DistinctUntilChanged 
                 |> Observable.subscribe(sendSpeed robot (fun () -> safeRemove n))

        let d2 = points 
                 |> Observable.map (fun ((l,r),_) -> 10 > abs (l - r)) 
                 |> Observable.DistinctUntilChanged
                 |> Observable.subscribe (sendKick robot)

        new Reactive.Disposables.CompositeDisposable(d1, d2)
    
    let updateSubscriptions (ids: int[]) =
            for id in ids do
                let i = activeSkeletons.FindIndex(fun (i,_) -> i = id)
                if i < 0 then
                    safeAddSub (id, joinPlayer id)

            for k,_ in activeSkeletons |> Linq.Enumerable.ToList do
                match ids |> Array.tryFind ((=) k) with
                | Some _ -> ()
                | None -> safeRemove k

    use subscriptionManager = skeltonsId.Subscribe updateSubscriptions
    let app = new Application()
    window.Closing.Add(fun _ -> sendExit 0)
    app.Run(window) |> ignore
    System.Console.ReadLine() |> ignore
    0