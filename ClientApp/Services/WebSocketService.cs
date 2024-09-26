using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class WebSocketService
{
    private ClientWebSocket _webSocket;

    public event Action<string> OnMessageReceived = delegate { };

    public async Task ConnectAsync()
    {
        _webSocket = new ClientWebSocket();
        await _webSocket.ConnectAsync(new Uri("ws://localhost:5247/ws"), CancellationToken.None);
        await ReceiveMessagesAsync();
    }

    public async Task SendMessageAsync(string message)
    {
        var messageBuffer = Encoding.UTF8.GetBytes(message);
        await _webSocket.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task DisconnectAsync()
    {
        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
    }

    private async Task ReceiveMessagesAsync()
    {
        var buffer = new byte[1024 * 4];
        while (_webSocket.State == WebSocketState.Open)
        {
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                OnMessageReceived.Invoke(message);
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
            }
        }
    }
}
