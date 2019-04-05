namespace Actors

open FSharp.Control.Tasks.V2
open Messages
open Orleankka
open Orleankka.FSharp

type ChatUser() =
    inherit ActorGrain()
    
    member this.Send roomName message =
        printfn "[server]: %s" message
        let room = ActorSystem.streamOf (this.System, "sms", roomName)
        room <! { UserName = this.Id
                  Text = message }
    
    interface IChatUser
    override this.Receive(message) =
        task { 
            match message with
            | :? ChatUserMessage as m -> 
                match m with
                | Join room -> 
                    let msg = sprintf "%s joined the room %s ..." this.Id room
                    do! this.Send room msg
                    return none()
                | Leave room -> 
                    let msg = sprintf "%s left the room %s!" this.Id room
                    do! this.Send room msg
                    return none()
                | Say(room, msg) -> 
                    let msg = sprintf "%s said: %s" this.Id msg
                    do! this.Send room msg
                    return none()
            | _ -> return unhandled()
        }
