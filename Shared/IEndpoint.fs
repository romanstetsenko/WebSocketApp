namespace Messages

open Orleankka.FSharp

type EndpointMsg = | SubscribeToTopic of string

type IEndpoint = 
    inherit IActorGrain<EndpointMsg>