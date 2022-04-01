namespace Caruti.Http;

public sealed class Request : IRequest
{
    public string Method { get; }
    public string Path { get; }
    public string Protocol { get; }
    public string? Query { get; }

    //TODO: Implement request body setter
    public byte[]? Body { get; }
    public IDictionary<string, string[]> Headers { get; } = new Dictionary<string, string[]>();

    public Request(ReadOnlyMemory<byte> buffer)
    {
        (Method, var currentBuffer) = GetNextWord(buffer).GetOrThrowException();
        (Path, currentBuffer) = GetNextWord(currentBuffer).GetOrThrowException();
        (Protocol, currentBuffer) = GetNextWord(currentBuffer).GetOrThrowException();

        Query = GetQueryString();

        var result = GetNextHeader(currentBuffer);
        while (result != null)
        {
            var (key, value, curBuffer) = result.Value;
            Headers.Add(key, new[] { value });
            result = GetNextHeader(curBuffer);
        }
    }

    private static (string, ReadOnlyMemory<byte>)? GetNextWord(ReadOnlyMemory<byte> buffer)
    {
        var span = buffer.Span;
        for (var i = 0; i < buffer.Length; i++)
        {
            var currentChar = (char)span[i];
            if (currentChar is not (' ' or '\r')) continue;

            var word = Encoding.UTF8.GetString(span[..i]);
            _ = word ?? throw new MalformedHttpRequestException();

            return (word, buffer[(i + 1)..]);
        }

        return null;
    }

    private static (string, string, ReadOnlyMemory<byte>)? GetNextHeader(ReadOnlyMemory<byte> buffer)
    {
        var span = buffer.Span;
        for (var i = 0; i < buffer.Length; i++)
        {
            var currentChar = (char)span[i];
            if (currentChar is not '\r') continue;

            var header = Encoding.UTF8.GetString(span[..i]);
            _ = header ?? throw new MalformedHttpRequestException();

            var doubleQuoteIndex = header.IndexOf(':');
            if (doubleQuoteIndex == -1)
                return null;

            var key = header[..doubleQuoteIndex];
            var value = header[(doubleQuoteIndex + 1)..].Trim();
            return (key, value, buffer[(i + 2)..]);
        }

        return null;
    }

    private string? GetQueryString()
    {
        var indexOfQuery = Path.IndexOf('?', StringComparison.Ordinal);
        if (indexOfQuery == -1) return null;

        //TODO: Implementar acesso a query string
        return null;
    }
}