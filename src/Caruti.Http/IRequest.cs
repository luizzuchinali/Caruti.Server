namespace Caruti.Http;

public interface IRequest
{
    public string Method { get; }
    public string Path { get; }
    public string Protocol { get; }
    public string? Query { get; }
    public byte[]? Body { get; }
    public IDictionary<string, string[]> Headers { get; }
}