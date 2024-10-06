using System.Net;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var connections = new List<WebSocket>();
app.UseWebSockets();
app.MapGet("/ws", async context =>
{
if (context.WebSockets.IsWebSocketRequest)
{
    var curName = context.Request.Query["name"];
    using var ws = await context.WebSockets.AcceptWebSocketAsync();
    connections.Add(ws);

    await Broadcast($"{curName} joined the room");
    await Broadcast($"{connections.Count} users connected");
    await RecieveMessage(ws, async (result, buffer) =>
    {
        if (result.MessageType == WebSocketMessageType.Text)
        {
            string message = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            await Broadcast(curName + ": " + message);
        }
        else if (result.MessageType == WebSocketMessageType.Close || ws.State == WebSocketState.Aborted)
        {
            connections.Remove(ws);
            await Broadcast($"{curName} left the room");
            await Broadcast($"{connections.Count} users connected");
            await ws.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
        });
    }
    else
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
    }
});

async Task RecieveMessage(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handlerMessage)
{
    var buffer = new byte[1024];
    while(socket.State == WebSocketState.Open)
    {
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        handlerMessage(result,buffer);
    }
}

async Task Broadcast(string message)
{
    var bytes = Encoding.UTF8.GetBytes(message);
    foreach(var socket in connections)
    {
        if(socket.State == WebSocketState.Open)
        {
            var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);
            await socket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);

        }
    }
}
app.Run();
