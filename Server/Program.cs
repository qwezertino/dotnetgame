using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dictionary to store all connected clients
ConcurrentDictionary<WebSocket, bool> allConnectedSockets = new ConcurrentDictionary<WebSocket, bool>();

// Enable WebSocket support
app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            // Add the connected socket to the dictionary
            allConnectedSockets.TryAdd(webSocket, true);

            // Handle the WebSocket communication
            await HandleWebSocketCommunication(webSocket, allConnectedSockets);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    else
    {
        await next();
    }
});

app.Run();

async Task HandleWebSocketCommunication(WebSocket webSocket, ConcurrentDictionary<WebSocket, bool> allConnectedSockets)
{
    var buffer = new byte[1024 * 4];
    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

    while (!result.CloseStatus.HasValue)
    {
        var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

        // Log received message to the server console (optional)
        Console.WriteLine($"Message received: {receivedMessage}");

        // Broadcast the message to all connected clients
        foreach (var socket in allConnectedSockets.Keys)
        {
            if (socket.State == WebSocketState.Open) //socket != webSocket &&
            {
                // Send the received message back to the client
                Console.WriteLine($"Sending message to client: {receivedMessage}");
                await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(receivedMessage)), result.MessageType, result.EndOfMessage, CancellationToken.None);
            }
        }

        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    }

    // Remove the socket when it is closed
    allConnectedSockets.TryRemove(webSocket, out _);

    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
}
