using System.Net.WebSockets;
using System.Text;

var ws = new ClientWebSocket();

String name;

Console.WriteLine("Input name:");
name = Console.ReadLine();

Console.WriteLine("Connecting to Server");
await ws.ConnectAsync(new Uri($"ws://localhost:5285/ws?name={name}"),CancellationToken.None);
Console.WriteLine("Connected");
var sendTask = Task.Run(async () =>
{
    while (true)
    {
        var message = Console.ReadLine();
        if (message == "exit")
        {
            break;
        }
        var bytes = Encoding.UTF8.GetBytes(message);
        await ws.SendAsync(new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }

});
var recieveTask = Task.Run(async () =>
{
    var buffer = new byte[1024*5 ];
    while (true)
    {
        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        if (result.MessageType == WebSocketMessageType.Close)
        {
            break;
        }
        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
        Console.WriteLine("Recieved:" + message);
    }
});

await Task.WhenAny(recieveTask,sendTask);

if(ws.State != WebSocketState.Open)
{
    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure,
        "closing",
        CancellationToken.None);
}
await Task.WhenAll(sendTask, recieveTask);