namespace WebSocketApp

module Middleware =
    open System
    open System.Text
    open System.Threading
    open System.Threading.Tasks
    open System.Net.WebSockets
    open Microsoft.AspNetCore.Http
    open FSharp.Control.Tasks.V2
    

    let mutable sockets = list<WebSocket>.Empty

    let private addSocket sockets socket = socket :: sockets

    let private removeSocket sockets socket =
        sockets
        |> List.choose (fun s -> if s <> socket then Some s else None)

    let private sendMessage =
        fun (socket : WebSocket) (message : string) ->
            task {
                let buffer = Encoding.UTF8.GetBytes(message)
                let segment = new ArraySegment<byte>(buffer)

                if socket.State = WebSocketState.Open then
                    do! socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None)
                else
                    sockets <- removeSocket sockets socket
            }
    
    let sendMessageToSockets =
        fun message ->
            task {
                for socket in sockets do
                    try
                        do! sendMessage socket message
                    with
                        | _ -> sockets <- removeSocket sockets socket
            }
    
    let keepConnectionAlive (webSocket:WebSocket) = 
            webSocket.ReceiveAsync(new ArraySegment<byte>(), CancellationToken.None).Wait()
            //let buffer : byte[] = Array.zeroCreate 1024
            //while webSocket.State = WebSocketState.Open do
                
            //    match res.MessageType with 
            //    | WebSocketMessageType.Binary -> 
            //        do! webSocket.CloseOutputAsync(WebSocketCloseStatus.InvalidPayloadData, "Sorry, WebSocketMessageType.Binary isn't supported yet.", CancellationToken.None)
            //    | WebSocketMessageType.Close -> 
            //        ()
            //    | WebSocketMessageType.Text ->
            //        let msg = System.Text.Encoding.UTF8.GetString(buffer, 0, res.Count)
            //        // publish msg
            //        ()
            //    | _ -> ()

    type WebSocketMiddleware(next : RequestDelegate) =
        member __.Invoke(ctx : HttpContext) =
            //async {
            //    if ctx.Request.Path = PathString("/ws") then
            //        match ctx.WebSockets.IsWebSocketRequest with
            //        | true ->
            //            let! webSocket = ctx.WebSockets.AcceptWebSocketAsync() |> Async.AwaitTask
            //            sockets <- addSocket sockets webSocket

            //            let buffer : byte[] = Array.zeroCreate 4096
            //            let! ct = Async.CancellationToken
                        
            //            webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct)
            //            |> Async.AwaitTask
            //            |> ignore

            //        | false -> ctx.Response.StatusCode <- 400
            //    else
            //        next.Invoke(ctx) |> Async.AwaitTask |> ignore
            //} |> Async.StartAsTask :> Task
            task {
                if ctx.Request.Path = PathString("/ws") then
                    match ctx.WebSockets.IsWebSocketRequest with
                    | true ->
                        //ctx.Response.StatusCode <- 200
                        //while true do
                        let! webSocket = ctx.WebSockets.AcceptWebSocketAsync()
                        sockets <- addSocket sockets webSocket
                        let buffer : byte[] = Array.zeroCreate 4096
                        let! res = webSocket.ReceiveAsync(new Memory<byte>(buffer), CancellationToken.None)
                        keepConnectionAlive webSocket
                        

                        //let sendBuffer = System.Text.Encoding.UTF8.GetBytes(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff"))
                        //do! webSocket.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, CancellationToken.None)
                        //let buffer : byte[] = Array.zeroCreate 4096
                        
                        //let! re = webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None)
                        //re |> ignore
                        //()
                        //for i in [1..100000] do
                        //    let sendBuffer = System.Text.Encoding.UTF8.GetBytes(string i + DateTime.Now.ToString(": yyyy-MM-dd HH:mm:ss.fffffff"))
                        //    do! webSocket.SendAsync(new ReadOnlyMemory<byte>(sendBuffer), WebSocketMessageType.Text, true, CancellationToken.None)
                    | false -> 
                        ctx.Response.StatusCode <- 400
                else
                    do! next.Invoke(ctx) 
            }