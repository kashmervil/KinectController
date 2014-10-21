module ObservableHelpers
open System
open System.Reactive.Linq

type Notifier<'T>() =
    let observers = new ResizeArray<IObserver<'T> >()
    let source = { new IObservable<'T> with
        member self.Subscribe observer =  
            lock observers <| fun () -> observers.Add(observer)
            { new IDisposable with 
                member this.Dispose() = lock observers <| fun () -> observers.Remove(observer) |> ignore }
              }

    member self.OnError e = 
            lock (observers)
            <| fun () -> observers.ForEach(fun obs -> obs.OnError e)
            observers.Clear()

    member self.OnNext (x: 'T) = 
        try 
            lock (observers)
            <| fun () -> observers.ForEach(fun obs -> obs.OnNext x) 
        with e -> self.OnError e

    member self.OnCompleted _ = 
            lock (observers)
            <| fun () -> observers.ForEach(fun obs -> obs.OnCompleted()) 
            observers.Clear()
    member self.Publish with get() = source

    interface IDisposable with
        member self.Dispose() = self.OnCompleted()

let Buffer (size:int) (skip: int) (x: IObservable<_>) = Observable.Buffer(x, size, skip)
    
let TakeDerivative(source: IObservable<(int*int)*(int*int)>) = 
        let previousKey = ref (0, 0)
        let notifier = new Notifier<int*int>()

        let observer = { new System.IObserver<_> with
                            member self.OnNext((_,x)) = 
                                try 
                                    let l,r = x
                                    let pL, pR = !previousKey
                                    previousKey := x 
                                    notifier.OnNext (l - pL, r - pR)

                                with e -> notifier.OnError e
                            member self.OnError e = notifier.OnError e
                            member self.OnCompleted() = notifier.OnCompleted()
                        }
        source.Subscribe(observer) |> ignore
        notifier.Publish
