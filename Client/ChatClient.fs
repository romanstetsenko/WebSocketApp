module Client.ChatClient

open FSharp.Control.Tasks.V2
open Messages
open Orleankka
open Orleankka.FSharp
open System

type ChatClient =
    { UserName : string
      User : ActorRef<ChatUserMessage>
      RoomName : string
      Room : StreamRef<ChatRoomMessage>
      Subscription : Option<StreamSubscription> }

let join (client : ChatClient) =
    task { 
        let! sb = client.Room.Subscribe(fun message -> 
                      if message.UserName <> client.UserName then printfn "%s" message.Text)
        do! client.User <! Join(client.RoomName)
        return { client with Subscription = Some sb }
    }

let leave (client : ChatClient) =
    task { 
        do! client.Subscription.Value.Unsubscribe()
        do! client.User <! Leave(client.RoomName)
    }

let say (client : ChatClient) (message : string) =
    task { do! client.User <! Say(client.RoomName, message) }

let rec handleUserInput client =
    task { 
        let message = Console.ReadLine()
        match message with
        | "::leave" -> do! leave client
        | _ -> 
            do! say client message
            return! handleUserInput client
    }

let startChatClient (system : IActorSystem) userName roomName =
    task { 
        let userActor = ActorSystem.typedActorOf<IChatUser, ChatUserMessage> (system, userName)
        let roomStream = ActorSystem.streamOf (system, "sms", roomName)
        
        let chatClient =
            { UserName = userName
              User = userActor
              RoomName = roomName
              Room = roomStream
              Subscription = None }
        printfn "Joining the room '%s'..." roomName
        let! joinedClient = join chatClient
        printfn "Joined the room '%s'..." roomName
        return! handleUserInput joinedClient
    }
