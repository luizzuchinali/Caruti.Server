namespace Caruti.Server.Tcp;

public class TcpConnection : Connection
{
    public TcpClient Client { get; private set; }

    private bool _disposed;

    public TcpConnection(TcpClient client)
        : base(client.GetStream())
    {
        Client = client;
    }

    public override void Close()
    {
        Connected = false;
        Dispose(true);
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Client.Close();
            }

            _disposed = true;
        }

        base.Dispose(disposing);
    }
}