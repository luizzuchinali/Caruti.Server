using System.Net.Mail;
using System.Net.Mime;
using MimeTypes;

namespace Caruti.Http;

public class Response : IResponse
{
    public string Protocol { get; }

    public byte[]? Body { get; private set; }
    public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();

    private readonly NetworkStream _stream;
    private readonly byte[] _newLineBytes = Encoding.UTF8.GetBytes(Environment.NewLine);

    public Response(string protocol, NetworkStream stream)
    {
        Protocol = protocol;
        _stream = stream;
    }

    private async Task WriteHeaders()
    {
        foreach (var (key, value) in Headers)
        {
            await _stream.WriteAsync(_newLineBytes);
            await _stream.WriteAsync(Encoding.UTF8.GetBytes($"{key}: {value}"));
        }

        await _stream.WriteAsync(_newLineBytes);
        await _stream.WriteAsync(_newLineBytes);
    }

    private async Task WriteResponse(byte[] data)
    {
        if (Body is not null)
            throw new InvalidOperationException("response body alredy set");

        Body = data;

        Headers.Add("Content-Length", data.Length.ToString());

        await WriteHeaders();
        await _stream.WriteAsync(data);
        await _stream.FlushAsync();
    }

    public async Task SendFile(byte[] fileBytes, string filename)
    {
        await _stream.WriteAsync(Encoding.UTF8.GetBytes($"{Protocol} {(int)EStatusCode.Ok} {EStatusCode.Ok}"));
        Headers.Add("Content-Type", MimeTypeMap.GetMimeType(filename));
        await WriteResponse(fileBytes);
    }
    
    public async Task SendHtml(string html, EStatusCode statusCode)
    {
        await _stream.WriteAsync(Encoding.UTF8.GetBytes($"{Protocol} {(int)statusCode} {statusCode}"));
        Headers.Add("Content-Type", "text/html; charset=UTF-8");
        await WriteResponse(Encoding.UTF8.GetBytes(html));
    }

    public async Task StatusCode(EStatusCode statusCode)
    {
        await _stream.WriteAsync(Encoding.UTF8.GetBytes($"{Protocol} {(int)statusCode} {statusCode}"));
        await WriteResponse(Array.Empty<byte>());
    }
}