namespace Messages

open Orleankka.FSharp
open Orleankka

type EndpointMsg = 
    | Topic of string
    | Attach of ObserverRef

type EndpointNotification = | Notification of string | Subscribed of string

type IEndpoint = 
    inherit IActorGrain<EndpointMsg>

type ReciterMsg = | Start
type IReciter =
    inherit IActorGrain<ReciterMsg>