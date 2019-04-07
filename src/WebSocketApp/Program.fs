module WebSocketApp.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Giraffe.Razor
open WebSocketApp.Models
open Middleware
open FSharp.Control.Tasks.V2
open Microsoft.AspNetCore.Http

// ---------------------------------
// Web app
// ---------------------------------
let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x
let port = "SERVER_PORT" |> tryGetEnv |> Option.map uint16 |> Option.defaultValue 5000us

let indexHandler (name : string) =
    let greetings = sprintf "Hello %s, from Giraffe!" name
    let model     = { Text = greetings; Port = port }
    razorHtmlView "Index" (Some model) None None

let handlePostMessage =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! message = ctx.BindJsonAsync<Message>()
            //do! sendMessageToSockets message.Text
            return! next ctx
        }


let webApp =
    choose [
        GET >=>
            choose [
                route "/" >=> indexHandler "world"
                routef "/hello/%s" indexHandler
            ]
        POST >=>
            choose [
                route "/message" >=> handlePostMessage
            ]
        setStatusCode 404 >=> text "Not Found" ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder.WithOrigins("http://localhost:8080")
           .AllowAnyMethod()
           .AllowAnyHeader()
           |> ignore

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IHostingEnvironment>()
    (match env.IsDevelopment() with
    | true  -> app.UseDeveloperExceptionPage()
    | false -> app.UseGiraffeErrorHandler errorHandler)
        .UseWebSockets()
        .UseMiddleware<WebSocketMiddleware>()
        .UseHttpsRedirection()
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    let sp  = services.BuildServiceProvider()
    let env = sp.GetService<IHostingEnvironment>()
    let viewsFolderPath = Path.Combine(env.ContentRootPath, "Views")
    services.AddRazorEngine viewsFolderPath |> ignore
    services.AddCors() |> ignore
    services.AddGiraffe() |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.AddFilter(fun l -> l.Equals LogLevel.Error)
           .AddConsole()
           .AddDebug() |> ignore

let configureBackEnd (services : IServiceCollection)= 
    let clientSystem = Client.ChatClientBuilder.create()
    services.AddSingleton(clientSystem) |> ignore

[<EntryPoint>]
let main _ =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    do Server.ServerBuilder.start()
    WebHostBuilder()
        .UseKestrel()
        .UseContentRoot(contentRoot)
        .UseIISIntegration()
        .UseWebRoot(webRoot)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .ConfigureServices(configureBackEnd)
        .UseUrls("http://localhost:" + port.ToString() + "/", "http://*:" + port.ToString() + "/")
        .ConfigureLogging(configureLogging)
        .Build()
        .Run()
    0
