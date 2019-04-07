namespace Client

module ChatClientBuilder =
    open Orleankka.Client
    open Orleans
    open Orleans.Configuration
    open Orleans.Hosting
    open Orleans.Runtime
    open System.Net
    open Messages

    let DemoClusterId = "localhost-demo"
    let DemoServiceId = "localhost-demo-service"
    let LocalhostGatewayPort = 30000
    let LocalhostSiloAddress = IPAddress.Loopback
    
    let create() =
        printfn "Bootstrapping and connecting client actor system"
        let cb = ClientBuilder()
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
            x.AddApplicationPart(typeof<IEndpoint>.Assembly).WithCodeGeneration() |> ignore) 
        |> ignore
        cb.UseOrleankka() |> ignore
        let client = cb.Build()
        client.Connect().Wait()
        printfn "Client actor system connected \n"
        client.ActorSystem()
