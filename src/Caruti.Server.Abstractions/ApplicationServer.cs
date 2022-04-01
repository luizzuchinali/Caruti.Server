namespace Caruti.Server.Abstractions;

public abstract class ApplicationServer : IApplicationServer
{
    public IPAddress Address { get; }
    public uint Port { get; }
    public Action? OnListen { get; set; }
    public Func<IConnection, CancellationToken, Task>? OnReceiveConnection { get; set; }

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
            if (OnReceiveConnection is null)
                continue;

            _ = OnReceiveConnection.Invoke(connection, cancellationToken)
                .ContinueWith(_ => connection.Close(), cancellationToken);
        }
    }
}