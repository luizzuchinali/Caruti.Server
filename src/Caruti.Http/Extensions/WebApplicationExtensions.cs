namespace Caruti.Http.Extensions;

public static class WebApplicationExtensions
{
    public static void Get(this WebApplication source, string path, Func<IRequest, IResponse, Task> action) =>
        source.Use(HttpMethod.Get, path, action);

    public static void Head(this WebApplication source, string path, Func<IRequest, IResponse, Task> action) =>
        source.Use(HttpMethod.Head, path, action);

    public static void Post(this WebApplication source, string path, Func<IRequest, IResponse, Task> action) =>
        source.Use(HttpMethod.Post, path, action);

    public static void Put(this WebApplication source, string path, Func<IRequest, IResponse, Task> action) =>
        source.Use(HttpMethod.Put, path, action);

    public static void Delete(this WebApplication source, string path, Func<IRequest, IResponse, Task> action) =>
        source.Use(HttpMethod.Get, path, action);

    public static void Options(this WebApplication source, string path, Func<IRequest, IResponse, Task> action) =>
        source.Use(HttpMethod.Options, path, action);

    public static void Trace(this WebApplication source, string path, Func<IRequest, IResponse, Task> action) =>
        source.Use(HttpMethod.Trace, path, action);

    public static void Patch(this WebApplication source, string path, Func<IRequest, IResponse, Task> action) =>
        source.Use(HttpMethod.Patch, path, action);
}