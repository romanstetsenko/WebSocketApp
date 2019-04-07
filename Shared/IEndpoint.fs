namespace Messages

open Orleankka.FSharp
open Orleankka

type EndpointMsg = 
    | SubscribeToTopic of string
    | Attach of ObserverRef

type EndpointNotification = | Text of string

type IEndpoint = 
    inherit IActorGrain<EndpointMsg>