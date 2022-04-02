namespace Caruti.Http;

public sealed class Request : IRequest
{
    public string Method { get; private set; }
    public string Path { get; private set; }
    public string Uri { get; private set; }
    public string Protocol { get; private set; }
    public string? Query { get; }

    //TODO: Implement request body setter
    public ReadOnlyMemory<byte>? Body { get; private set; }
    public IReadOnlyDictionary<string, string> Headers { get; private set; }

    private Request(string method,
        string uri,
        string protocol,
        string? query,
        IReadOnlyDictionary<string, string> headers,
        byte[] body)
    {
        Method = method;
        Uri = uri;
        var queryStringInitializerIndex = Uri.IndexOf('?');
        Path = queryStringInitializerIndex == -1 ? Uri : Uri[..queryStringInitializerIndex];
        Protocol = protocol;
        Query = query;
        Headers = headers;
        Body = body;
    }

    public static async Task<Request> Create(NetworkStream stream)
    {
        var buffer = new byte[1024 * 8];
        await stream.ReadAsync(buffer);

        (var method, buffer) = GetNextWord(buffer).GetOrThrowException();
        (var path, buffer) = GetNextWord(buffer).GetOrThrowException();
        (var protocol, buffer) = GetNextWord(buffer).GetOrThrowException();

        var query = GetQueryString(path);
        var headers = GetHeaders(buffer);

        //TODO: implement body
        return new Request(method, path, protocol, query, headers, new byte[255]);
    }

    private static (string, byte[])? GetNextWord(byte[] buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            var currentChar = (char)buffer[i];
            if (currentChar is not (' ' or '\r')) continue;

            var word = Encoding.UTF8.GetString(buffer[..i]);
            _ = word ?? throw new MalformedHttpRequestException();

            return (word, buffer[(i + 1)..]);
        }

        return null;
    }

    private static (string, string, byte[])? GetNextHeader(byte[] buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            var currentChar = (char)buffer[i];
            if (currentChar is not '\r') continue;

            var header = Encoding.UTF8.GetString(buffer[..i]);
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

    private static IReadOnlyDictionary<string, string> GetHeaders(byte[] buffer)
    {
        var result = GetNextHeader(buffer);
        var headers = new Dictionary<string, string>();
        while (result != null)
        {
            (var key, var value, buffer) = result.Value;
            headers.Add(key, value);
            result = GetNextHeader(buffer);
        }

        return headers;
    }

    private static string? GetQueryString(string path)
    {
        var indexOfQuery = path.IndexOf('?', StringComparison.Ordinal);
        if (indexOfQuery == -1) return null;

        //TODO: Implementar acesso a query string
        return null;
    }
}