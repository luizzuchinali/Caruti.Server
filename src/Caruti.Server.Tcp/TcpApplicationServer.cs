namespace Caruti.Server.Tcp;

public class TcpApplicationServer : ApplicationServer
{
    private readonly TcpListener _listener;

    public TcpApplicationServer(string address, uint port = 8080) :
        base(address, port)
    {
        _listener = new TcpListener(IPAddress.Parse(address), (int)port);
    }

    protected override void Start() => _listener.Start();

    protected override async Task<IConnection> AcceptConnection()
    {
        var tcpClient = await _listener.AcceptTcpClientAsync();
        return new TcpConnection(tcpClient);
    }
}