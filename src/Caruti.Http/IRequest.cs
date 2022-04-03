namespace Caruti.Http;

public interface IRequest
{
    public HttpMethod Method { get; }
    public string Path { get; }
    public string Uri { get; }
    public string Protocol { get; }
    public string? Query { get; }
    public byte[] Body { get; }
    public IReadOnlyDictionary<string, string> Headers { get; }

    void SetParams(string template);

    T GetParam<T>(string paramName)
        where T : struct, ISpanFormattable, IComparable, IComparable<T>, IEquatable<T>;
}