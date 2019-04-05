namespace Client

module ChatClientBuilder =
    open Client.ChatClient
    open Messages
    open Orleankka.Client
    open Orleans
    open Orleans.Configuration
    open Orleans.Hosting
    open Orleans.Runtime
    open System
    open System.Net
    
    let DemoClusterId = "localhost-demo"
    let DemoServiceId = "localhost-demo-service"
    let LocalhostGatewayPort = 30000
    let LocalhostSiloAddress = IPAddress.Loopback
    
    let create() =
        printfn "Please wait until Chat Server has completed boot and then press enter. \n"
        Console.ReadLine() |> ignore
        let cb = new ClientBuilder()
        cb.Configure<ClusterOptions>(fun (options : ClusterOptions) -> 
            options.ClusterId <- DemoClusterId
            options.ServiceId <- DemoServiceId)
        |> ignore
        cb.UseStaticClustering
            (fun (options : StaticGatewayListProviderOptions) -> 
            options.Gateways.Add
                (IPEndPoint(LocalhostSiloAddress, LocalhostGatewayPort).ToGatewayUri())) |> ignore
        cb.AddSimpleMessageStreamProvider("sms") |> ignore
        cb.ConfigureApplicationParts
            (fun x -> 
            x.AddApplicationPart(typeof<IChatUser>.Assembly).WithCodeGeneration() |> ignore) 
        |> ignore
        cb.UseOrleankka() |> ignore
        let client = cb.Build()
        client.Connect().Wait()
        printfn "Enter your user name..."
        let userName = Console.ReadLine()
        printfn "Enter a room which you'd like to join..."
        let roomName = Console.ReadLine()
        let system = client.ActorSystem()
        let t = startChatClient system userName roomName
        t.Wait()
        Console.ReadLine() |> ignore
        system
