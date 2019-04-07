namespace Server

module ServerBuilder =
    open Orleankka.Cluster
    open Orleans
    open Orleans.ApplicationParts
    open Orleans.Configuration
    open Orleans.Hosting
    open Orleans.Storage
    open System.Net
    open System.Reflection
    open Messages
    
    let start() =
        let demoClusterId = "localhost-demo"
        let demoServiceId = "localhost-demo-service"
        let localhostSiloPort = 11111
        let localhostGatewayPort = 30000
        let localhostSiloAddress = IPAddress.Loopback
        printfn "Running demo. Booting cluster might take some time"
        let configureAssemblies (apm : IApplicationPartManager) =
            apm.AddApplicationPart(typeof<IEndpoint>.Assembly).WithCodeGeneration() |> ignore
            apm.AddApplicationPart(typeof<IPEndPoint>.Assembly).WithCodeGeneration() |> ignore
            apm.AddApplicationPart(typeof<MemoryGrainStorage>.Assembly).WithCodeGeneration() |> ignore
            apm.AddApplicationPart(Assembly.GetExecutingAssembly()).WithCodeGeneration() |> ignore
        
        let sb = SiloHostBuilder()
        sb.Configure<ClusterOptions>(fun (options : ClusterOptions) -> 
            options.ClusterId <- demoClusterId
            options.ServiceId <- demoServiceId)
        |> ignore
        sb.UseDevelopmentClustering
            (fun (options : DevelopmentClusterMembershipOptions) -> 
            options.PrimarySiloEndpoint <- IPEndPoint(localhostSiloAddress, localhostSiloPort)) 
        |> ignore
        sb.ConfigureEndpoints(localhostSiloAddress, localhostSiloPort, localhostGatewayPort) 
        |> ignore
        sb.AddMemoryGrainStorageAsDefault() |> ignore
        sb.AddMemoryGrainStorage("PubSubStore") |> ignore
        sb.AddSimpleMessageStreamProvider("sms") |> ignore
        sb.UseInMemoryReminderService() |> ignore
        configureAssemblies |> sb.ConfigureApplicationParts |> ignore
        sb.UseOrleankka() |> ignore
        sb.Build().StartAsync().Wait()
        printfn "Finished booting cluster \n"
