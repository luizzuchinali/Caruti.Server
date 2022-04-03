
namespace Caruti.Http;

public sealed class Request : IRequest
{
    public HttpMethod Method { get; private set; }
    public string Path { get; private set; }
    public string Uri { get; private set; }
    public string Protocol { get; private set; }
    public string? Query { get; }

    //not alocate if don't have params in template
    private IDictionary<string, object>? _params;

    //TODO: Implement per request cancellationToken
    public byte[] Body { get; }
    public IReadOnlyDictionary<string, string> Headers { get; private set; }

    private Request(string method,
        string uri,
        string protocol,
        string? query,
        IReadOnlyDictionary<string, string> headers,
        ref byte[] body)
    {
        Method = new HttpMethod(method);
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
        var buffer = new byte[1024 * 4];
        await stream.ReadAsync(buffer);

        var method = GetNextWord(ref buffer).GetOrThrowException();
        var path = GetNextWord(ref buffer).GetOrThrowException();
        var protocol = GetNextWord(ref buffer).GetOrThrowException();

        var query = GetQueryString(path);
        var headers = GetHeaders(ref buffer);

        //skip two new line bytes
        buffer = buffer[2..];

        var bodySize = int.Parse(headers["Content-Length"]);
        if (buffer.Length >= bodySize)
        {
            buffer = buffer[..bodySize];
            return new Request(method, path, protocol, query, headers, ref buffer);
        }

        var bodyBuffer = new byte[bodySize - buffer.Length];
        await stream.ReadAsync(bodyBuffer);
        Array.Copy(buffer, 0, bodyBuffer, 0, buffer.Length);

        return new Request(method, path, protocol, query, headers, ref bodyBuffer);
    }

    public void SetParams(string template)
    {
        _params ??= new Dictionary<string, object>();

        var templateParts = template.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var pathParts = Path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < templateParts.Length; i++)
        {
            var isParam = WebApplication.PathWithParamRegex.IsMatch(templateParts[i]);
            if (!isParam) continue;

            //Remove { and } from template
            var lastBracketIndex = templateParts[i].Length - 1;
            templateParts[i] = templateParts[i][1..lastBracketIndex];
            _params.Add(templateParts[i], pathParts[i]);
        }
    }

    public T GetParam<T>(string paramName)
        where T : struct, ISpanFormattable, IComparable, IComparable<T>, IEquatable<T>
    {
        //TODO: Implement especific exception
        if (_params == null)
            throw new InvalidOperationException();

        var value = _params[paramName];
        var type = typeof(T);
        var parse = type.GetMethods(BindingFlags.Public | BindingFlags.Static).First(x => x.Name.Equals("Parse"));
        return (T)parse.Invoke(null, new[] { value })!;
    }

    private static string? GetNextWord(ref byte[] buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            var currentChar = (char)buffer[i];
            if (currentChar is not (' ' or '\r')) continue;

            var word = Encoding.UTF8.GetString(buffer[..i]);
            _ = word ?? throw new MalformedHttpRequestException();

            buffer = buffer[(i + 1)..];
            return word;
        }

        return null;
    }

    private static (string, string)? GetNextHeader(ref byte[] buffer)
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

            buffer = buffer[(i + 2)..];
            return (key, value);
        }

        return null;
    }

    private static IReadOnlyDictionary<string, string> GetHeaders(ref byte[] buffer)
    {
        var result = GetNextHeader(ref buffer);
        var headers = new Dictionary<string, string>();
        while (result != null)
        {
            var (key, value) = result.Value;
            headers.Add(key, value);
            result = GetNextHeader(ref buffer);
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