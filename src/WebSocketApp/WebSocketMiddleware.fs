namespace WebSocketApp

open System.Net.WebSockets
open FSharp.Control.Tasks.V2.ContextInsensitive
open System

module Middleware =
    open Microsoft.AspNetCore.Http
    open Orleankka.Client
    open Giraffe.Core
    open Orleankka.FSharp
    open Messages

    let keepConnectionAlive (actorSystem: IClientActorSystem) (id, ct) (ws : WebSocket) =
        
        let actor = ActorSystem.typedActorOf<IEndpoint, EndpointMsg>(actorSystem, id)
        let notify (event:obj) = 
            match event with
            | :? EndpointNotification as notification ->
                match notification with 
                | Text text-> 
                    let sendBuffer = System.Text.Encoding.UTF8.GetBytes(text)
                    ws.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, ct).Wait()
            | _ -> () //sorry

        task { 
            use! observable = actorSystem.CreateObservable()
            do! actor <! (Attach observable.Ref)
            // dis Subscribe method is inconvenient 
            use _ = observable.Subscribe ( notify )//fun (x:obj) -> x :?> EndpointNotification |>
            
            let buffer : byte [] = Array.zeroCreate 1024
            while ws.State = WebSocketState.Open do
                let! res = ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct)
                match res.MessageType with
                | WebSocketMessageType.Text -> 
                    let msg =
                        System.Text.Encoding.UTF8.GetString
                            (buffer, 0, res.Count)
                    actor.Notify(EndpointMsg.SubscribeToTopic msg)
                | WebSocketMessageType.Binary -> 
                    do! ws.CloseOutputAsync
                            (WebSocketCloseStatus.InvalidPayloadData, 
                             "Sorry, WebSocketMessageType.Binary isn't supported yet.", 
                             ct)
                | WebSocketMessageType.Close | _ -> ()
        }
 
    type WebSocketMiddleware(next : RequestDelegate) =
        member __.Invoke(ctx : HttpContext) =
            task { 
                if ctx.Request.Path = PathString("/ws") then 
                    match ctx.WebSockets.IsWebSocketRequest with
                    | true -> 
                        let! webSocket = ctx.WebSockets.AcceptWebSocketAsync()
                        do! keepConnectionAlive 
                                (ctx.GetService<IClientActorSystem>())
                                (ctx.Connection.Id, ctx.RequestAborted) 
                                webSocket
                    | false -> ctx.Response.StatusCode <- 400
                else do! next.Invoke(ctx)
            }
