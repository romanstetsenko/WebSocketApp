﻿namespace Server

module ServerBuilder =
    open Messages
    open Orleankka.Cluster
    open Orleans
    open Orleans.ApplicationParts
    open Orleans.Configuration
    open Orleans.Hosting
    open Orleans.Storage
    open System.Net
    open System.Reflection
    
    let start() =
        let DemoClusterId = "localhost-demo"
        let DemoServiceId = "localhost-demo-service"
        let LocalhostSiloPort = 11111
        let LocalhostGatewayPort = 30000
        let LocalhostSiloAddress = IPAddress.Loopback
        printfn "Running demo. Booting cluster might take some time ...\n"
        let configureAssemblies (apm : IApplicationPartManager) =
            apm.AddApplicationPart(typeof<IChatUser>.Assembly).WithCodeGeneration() |> ignore
            apm.AddApplicationPart(typeof<MemoryGrainStorage>.Assembly).WithCodeGeneration() 
            |> ignore
            apm.AddApplicationPart(Assembly.GetExecutingAssembly()).WithCodeGeneration() |> ignore
        
        let sb = SiloHostBuilder()
        sb.Configure<ClusterOptions>(fun (options : ClusterOptions) -> 
            options.ClusterId <- DemoClusterId
            options.ServiceId <- DemoServiceId)
        |> ignore
        sb.UseDevelopmentClustering
            (fun (options : DevelopmentClusterMembershipOptions) -> 
            options.PrimarySiloEndpoint <- IPEndPoint(LocalhostSiloAddress, LocalhostSiloPort)) 
        |> ignore
        sb.ConfigureEndpoints(LocalhostSiloAddress, LocalhostSiloPort, LocalhostGatewayPort) 
        |> ignore
        sb.AddMemoryGrainStorageAsDefault() |> ignore
        sb.AddMemoryGrainStorage("PubSubStore") |> ignore
        sb.AddSimpleMessageStreamProvider("sms") |> ignore
        sb.UseInMemoryReminderService() |> ignore
        sb.ConfigureApplicationParts(fun x -> configureAssemblies x) |> ignore
        sb.UseOrleankka() |> ignore
        use host = sb.Build()
        host.StartAsync().Wait()
        printfn "Finished booting cluster...\n"
        System.Console.ReadLine() |> ignore
