namespace Caruti.Server.Abstractions.Interfaces;

public interface IApplicationServer
{
    public IPAddress Address { get; }
    public uint Port { get; }
    public Action? OnListen { get; set; }
    public Func<IConnection, CancellationToken, Task>? OnReceiveConnection { get; set; }

    Task Listen(CancellationToken cancellationToken);
}