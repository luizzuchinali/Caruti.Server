namespace Caruti.Server.Abstractions;

public abstract class Connection : IConnection
{
    public NetworkStream Stream { get; protected set; }
    public bool Connected { get; set; }

    protected Connection(NetworkStream stream)
    {
        Stream = stream;
        Connected = true;
    }
    
    public abstract void Close();

    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            Stream.Dispose();
        }

        _disposed = true;
    }
}