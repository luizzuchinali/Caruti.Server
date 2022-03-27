namespace Caruti.Server.Abstractions.Interfaces;

public interface IConnection : IDisposable
{
    public NetworkStream Stream { get; }
    public bool Connected { get; }

    void Close();
}