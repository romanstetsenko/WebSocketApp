module WsClientsSwarm

open FSharp.Control.Tasks.V2
open System.Net.WebSockets
open System.Threading.Tasks
open System.Threading
open System

let createOne ct id =
    let ws = new ClientWebSocket()
    let mutable count = 0
    task {
        do! ws.ConnectAsync(Uri("ws://localhost:5000/ws"), ct)
        let sendBuffer = System.Text.Encoding.UTF8.GetBytes("Volume1.txt")
        do! ws.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, ct)
        let buffer : byte [] = Array.zeroCreate 1024
        while ws.State = WebSocketState.Open do
            let! _res = ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct)
            count <- count + 1
            if count % 100 = 0 then printfn "[%A] ClientWebSocket #%i got %i messages" (DateTime.Now.ToString("hh:mm:ss.fffffff")) id count
    }

let createAndRun ct n =
    Thread.Sleep (10*1000)
    let arr = Array.init n (createOne ct) |> Array.map (fun  t -> t :> Task)
    Task.WaitAll(arr) //|>  Async.AwaitTask |> Async.Start
