namespace Actors

open FSharp.Control.Tasks.V2
open Messages
open Orleankka
open Orleankka.FSharp
open System
open System.Resources

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
                    sprintf "%s subscribed to the topic %s ..." this.Id topic
                    |> Subscribed
                    |> observers.Notify
                    return none()
                | Attach observer -> 
                    observers.Add(observer)
                    return none()
            | _ -> return unhandled()
        }

type Reciter ()  = 
    inherit ActorGrain()
    //do self.Timers.Register("Reciter", TimeSpan.FromMilliseconds(1000.))
    let resName = 
        typeof<Reciter>.Assembly.GetManifestResourceNames() 
        |> Array.find (fun (name:string) -> name.EndsWith("Volume1.txt"))
    interface IReciter
    override this.Receive(message) =
        task { 
            match message with
            | :? Timer as x ->
                return none()
            | :? ReciterMsg as _msg ->
                this.Timers.Register("Reciter", TimeSpan.FromMilliseconds(1000.))
                return none()
            | _ -> 
                return unhandled()
        }
