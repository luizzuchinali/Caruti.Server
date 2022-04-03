using System.Text.Json;

namespace Caruti.Http.Extensions;

public static class RequestExtensions
{
    public static async Task<T> FromJson<T>(this IRequest request, JsonSerializerOptions? options = default)
        where T : class
    {
        await using var stream = new MemoryStream(request.Body);
        options ??= new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var data = await JsonSerializer.DeserializeAsync<T>(stream, options);
        return data.GetOrThrowException();
    }
}