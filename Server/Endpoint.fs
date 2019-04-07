﻿namespace Actors

open FSharp.Control.Tasks.V2
open Messages
open Orleankka
open Orleankka.FSharp

    
//    member this.Send roomName message =
//        printfn "[server]: %s" message
//        let room = ActorSystem.streamOf (this.System, "sms", roomName)
//        room <! { UserName = this.Id
//                  Text = message }
    
type Endpoint () = 
    inherit  ActorGrain()
    interface IEndpoint
    override this.Receive(message) =
        task { 
            match message with
            | :? EndpointMsg as msg ->
                match msg with
                | SubscribeToTopic topic ->
                    let msg = sprintf "%s subscribed to the topic %s ..." this.Id topic
                    return none()
            | _ -> return unhandled()
        }