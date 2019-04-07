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
    open FSharp.Control.Tasks.V2
    open System.Threading
    open System.Threading.Tasks
    open Microsoft.Extensions.DependencyInjection
    open System
    open Orleankka
    open Orleankka.FSharp
    
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
        let aa = System.Func<IServiceProvider, CancellationToken, Task>(fun s  _ct ->
            task {

                let system = s.GetRequiredService<IActorSystem>()
                let randomId = Guid.NewGuid().ToString()
                let actor = ActorSystem.typedActorOf<IReciter, ReciterMsg>(system, randomId)
                do! actor <! Start
                ()
            } :> _
        )
        sb.AddStartupTask(aa) |> ignore
        sb.UseOrleankka() |> ignore
        sb.Build().StartAsync().Wait()
        printfn "Finished booting cluster \n"
