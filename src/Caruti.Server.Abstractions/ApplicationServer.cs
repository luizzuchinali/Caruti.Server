using Caruti.Server.Abstractions.Interfaces;

namespace Caruti.Server.Abstractions;

public abstract class ApplicationServer : IApplicationServer
{
    public IPAddress Address { get; }
    public uint Port { get; }
    public Action? OnListen { get; set; }
    public Func<IConnection, Task>? OnReceiveConnection { get; set; }

    protected ApplicationServer(string address, uint port = 8080)
    {
        Address = IPAddress.Parse(address);
        Port = port;
    }

    protected abstract void Start();
    protected abstract Task<IConnection> AcceptConnection();

    public async Task Listen(CancellationToken cancellationToken = default)
    {
        Start();
        OnListen?.Invoke();
        while (!cancellationToken.IsCancellationRequested)
        {
            var connection = await AcceptConnection();
            var awaiter = Task.Run(async () =>
            {
                if (OnReceiveConnection is null)
                    return;
                await OnReceiveConnection.Invoke(connection);
            }, cancellationToken).GetAwaiter();
            awaiter.OnCompleted(() => { connection.Close(); });
        }
    }
}