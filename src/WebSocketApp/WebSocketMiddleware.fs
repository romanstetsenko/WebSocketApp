namespace WebSocketApp

open System.Net.WebSockets
open System.Threading
open FSharp.Control.Tasks.V2.ContextInsensitive
open System

type SocketEndpoint =
    { id : string
      ws : WebSocket
      ct : CancellationToken }

type MsgHub(id, ct, notify) =
    interface IDisposable with
        member __.Dispose() = printfn "[%s] Must unsubscribe" id
    
    member __.Handle cmd = task { printfn "[%s] msg '%A' sent" id cmd }

module MsgHub =
    ()

module SocketEndpoint =
    let create (id, ws, ct) =
        printfn "[%s connection established]" id
        { id = id
          ws = ws
          ct = ct }
    
    let say (text : string) se =
        printfn "[%s replied] %s" se.id text
        let sendBuffer = System.Text.Encoding.UTF8.GetBytes(text)
        se.ws.SendAsync
            (new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, 
             se.ct)
    
    let handle msg se = task { printfn "[%s received] %s" se.id msg }
    
    let close reason se =
        printfn "[%s closed:] because %s" se.id reason
        se.ws.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, se.ct)

module Middleware =
    open Microsoft.AspNetCore.Http
    open Orleankka.Client
    open Giraffe.Core
    open Orleankka.FSharp
    open Messages

    let keepConnectionAlive3 (actorSystem: IClientActorSystem) (id, ct) (ws : WebSocket) =
        let actor = ActorSystem.typedActorOf<IEndpoint, EndpointMsg>(actorSystem, id)
        actor.Tell(EndpointMsg.SubscribeToTopic "PING").Wait()
        let notify event =
            printfn "[%s replied] %s" id event
            let sendBuffer = System.Text.Encoding.UTF8.GetBytes(event)
            ws.SendAsync
                (new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, 
                 true, ct)
        task { 
            let buffer : byte [] = Array.zeroCreate 1024
            while ws.State = WebSocketState.Open do
                let! res = ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct)
                match res.MessageType with
                | WebSocketMessageType.Text -> 
                    let cmd =
                        System.Text.Encoding.UTF8.GetString
                            (buffer, 0, res.Count)
                    actor.Notify(EndpointMsg.SubscribeToTopic cmd)
                | WebSocketMessageType.Binary -> 
                    do! ws.CloseOutputAsync
                            (WebSocketCloseStatus.InvalidPayloadData, 
                             "Sorry, WebSocketMessageType.Binary isn't supported yet.", 
                             ct)
                | WebSocketMessageType.Close | _ -> ()
        }
    //let keepConnectionAlive2 (id, ct) (ws : WebSocket) =
    //    let notify event =
    //        printfn "[%s replied] %s" id event
    //        let sendBuffer = System.Text.Encoding.UTF8.GetBytes(event)
    //        ws.SendAsync
    //            (new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, 
    //             true, ct)
    //    task { 
    //        use mh = new MsgHub(id, ct, notify)
    //        let buffer : byte [] = Array.zeroCreate 1024
    //        while ws.State = WebSocketState.Open do
    //            let! res = ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct)
    //            match res.MessageType with
    //            | WebSocketMessageType.Text -> 
    //                let cmd =
    //                    System.Text.Encoding.UTF8.GetString
    //                        (buffer, 0, res.Count)
    //                do! mh.Handle cmd
    //            | WebSocketMessageType.Binary -> 
    //                do! ws.CloseOutputAsync
    //                        (WebSocketCloseStatus.InvalidPayloadData, 
    //                         "Sorry, WebSocketMessageType.Binary isn't supported yet.", 
    //                         ct)
    //            | WebSocketMessageType.Close | _ -> ()
    //    }
    
    //let keepConnectionAlive (connectionId, ct) (webSocket : WebSocket) =
    //    task { 
    //        let se = SocketEndpoint.create (connectionId, webSocket, ct)
    //        let buffer : byte [] = Array.zeroCreate 1024
    //        while webSocket.State = WebSocketState.Open do
    //            let! res = webSocket.ReceiveAsync
    //                           (new ArraySegment<byte>(buffer), ct)
    //            match res.MessageType with
    //            | WebSocketMessageType.Text -> 
    //                let msg =
    //                    System.Text.Encoding.UTF8.GetString
    //                        (buffer, 0, res.Count)
    //                do! SocketEndpoint.handle msg se
    //                do! SocketEndpoint.say ("NO! " + msg) se
    //            | WebSocketMessageType.Binary -> 
    //                do! webSocket.CloseOutputAsync
    //                        (WebSocketCloseStatus.InvalidPayloadData, 
    //                         "Sorry, WebSocketMessageType.Binary isn't supported yet.", 
    //                         ct)
    //            | WebSocketMessageType.Close | _ -> 
    //                do! SocketEndpoint.close "requested by client" se
    //    }
    
    type WebSocketMiddleware(next : RequestDelegate) =
        member __.Invoke(ctx : HttpContext) =
            task { 
                if ctx.Request.Path = PathString("/ws") then 
                    match ctx.WebSockets.IsWebSocketRequest with
                    | true -> 
                        let! webSocket = ctx.WebSockets.AcceptWebSocketAsync()
                        do! keepConnectionAlive3 
                                (ctx.GetService<IClientActorSystem>())
                                (ctx.Connection.Id, ctx.RequestAborted) 
                                webSocket
                    | false -> ctx.Response.StatusCode <- 400
                else do! next.Invoke(ctx)
            }
