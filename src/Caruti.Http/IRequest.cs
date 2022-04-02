namespace Caruti.Http;

public interface IRequest
{
    public string Method { get; }
    public string Path { get; }
    public string Uri { get; }
    public string Protocol { get; }
    public string? Query { get; }
    public ReadOnlyMemory<byte>? Body { get; }
    public IReadOnlyDictionary<string, string> Headers { get; }
}