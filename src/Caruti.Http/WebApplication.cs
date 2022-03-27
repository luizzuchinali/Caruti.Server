using Caruti.Server.Abstractions.Interfaces;

namespace Caruti.Http;

public class WebApplication
{
    private IApplicationServer Server { get; }

    public WebApplication(IApplicationServer server)
    {
        Server = server;
        Server.OnReceiveConnection = ReceiveConnection;
    }

    public Task Listen(CancellationToken cancellationToken = default)
    {
        return Server.Listen(cancellationToken);
    }

    private async Task ReceiveConnection(IConnection connection)
    {
        await using var stream = connection.Stream;
        while (connection.Connected)
        {
            var buffer = new byte[1024 * 8];
            var bufferIndex = 0;
            while (stream.Socket.Available != 0)
            {
                buffer[bufferIndex] = (byte)stream.ReadByte();
                bufferIndex++;
            }

            if (bufferIndex == 0)
                continue;

            WriteLine("Buffer length: " + buffer.Length);
            WriteLine("Available data size: " + bufferIndex);

            var readOnlyBuffer = new ReadOnlyMemory<byte>(buffer);
            var request = new Request(readOnlyBuffer);

            WriteLine(request.Method);
            WriteLine(request.Path);
            WriteLine(request.Query);

            const string content = "<h1>Hello world</h1>";
            var writer = new StreamWriter(stream);
            await writer.WriteAsync("HTTP/1.1 200 OK");
            await writer.WriteAsync(Environment.NewLine);
            await writer.WriteAsync("Content-Type: text/html; charset=UTF-8");
            await writer.WriteAsync(Environment.NewLine);
            await writer.WriteAsync("Content-Length: " + content.Length);
            await writer.WriteAsync(Environment.NewLine);
            await writer.WriteAsync(Environment.NewLine);
            await writer.WriteAsync(content);
            await writer.FlushAsync();
        }
    }
}