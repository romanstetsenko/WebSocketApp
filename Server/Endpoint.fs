namespace Actors

open FSharp.Control.Tasks.V2
open Messages
open Orleankka
open Orleankka.FSharp
open System
open System.IO

type Endpoint () = 
    inherit  ActorGrain()
    
    let observers = ObserverCollection() :> IObserverCollection
    
    interface IEndpoint
    override this.Receive(message) =
        task { 
            match message with
            | :? EndpointMsg as msg ->
                match msg with
                | Topic topic ->
                    do! this.System.StreamOf("sms", topic).Subscribe(this)
                    return none()
                | Attach observer -> 
                    observers.Add(observer)
                    return none()
            | :? string as msg -> 
                msg |> Subscribed |> observers.Notify
                return none()
            | _ -> return unhandled()
        }

type Reciter () = 
    inherit ActorGrain()
    
    let theBook = 
        let ass = typeof<Reciter>.Assembly
        use stream = 
            ass.GetManifestResourceNames() 
            |> Array.find (fun (name:string) -> name.EndsWith("Volume1.txt"))
            |> ass.GetManifestResourceStream
        use sr = new StreamReader(stream)
        sr.ReadToEnd().Split(" ")

    let mutable cursor = 0
    let length = theBook.Length

    interface IReciter
    override this.Receive(message) =
        task { 
            match message with
            | :? Timer as _t ->
                do! theBook.[cursor % length] |> this.System.StreamOf("sms", "Volume1.txt").Push
                cursor <- cursor + 1
                return none()
            | :? ReciterMsg as _msg ->
                this.Timers.Register("Reciter", TimeSpan.FromMilliseconds(1000.), TimeSpan.FromMilliseconds(50.))
                return none()
            | _ -> 
                return unhandled()
        }
