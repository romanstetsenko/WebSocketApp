namespace WebSocketApp.Models

[<CLIMutable>]
type Message =
    {
        Text : string
        Port: uint16
    }