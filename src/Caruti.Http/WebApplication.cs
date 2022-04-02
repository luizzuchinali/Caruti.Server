namespace Caruti.Http;

public class WebApplication : IWebApplication
{
    private IApplicationServer Server { get; }

    private readonly IDictionary<string, Func<IRequest, IResponse, Task>> _routes;

    private readonly ICollection<Func<IRequest, IResponse, Func<Task>, Task>> _middlewares;

    private CancellationToken _cancellationToken = CancellationToken.None;

    public WebApplication(IApplicationServer server)
    {
        Server = server;
        Server.OnReceiveConnection = ReceiveConnection;
        _middlewares = new List<Func<IRequest, IResponse, Func<Task>, Task>>();
        _routes = new Dictionary<string, Func<IRequest, IResponse, Task>>();
    }

    public Task Listen(CancellationToken cancellationToken = default)
    {
        _cancellationToken = cancellationToken;

        Use(async (request, response) =>
        {
            if (_routes.ContainsKey(request.Path))
            {
                var route = _routes[request.Path];
                await route.Invoke(request, response);
            }
            else
            {
                await response.StatusCode(EStatusCode.NotFound);
            }
        });

        return Server.Listen(cancellationToken);
    }

    private async Task ReceiveConnection(IConnection connection, CancellationToken cancellationToken)
    {
        var stream = connection.Stream;

        //TODO: add configuration capability to change default request size
        while (connection.Connected && !cancellationToken.IsCancellationRequested)
        {
            if (stream.Socket.Available == 0)
                continue;

            var request = await Request.Create(stream);
            var response = new Response(request.Protocol, stream);

            try
            {
                await InvokeMiddlewareChain(request, response, _middlewares.GetEnumerator());
            }
            catch (Exception e)
            {
                connection.Close();
                WriteLine(e);
                continue;
            }

            //TODO: Change how server reacts for Connection header
            if (!request.Headers.ContainsKey("Connection") || !request.Headers["Connection"].Contains("keep-alive"))
                break;
        }
    }

    private static async Task InvokeMiddlewareChain(
        IRequest request,
        IResponse response,
        IEnumerator<Func<IRequest, IResponse, Func<Task>, Task>> enumerator)
    {
        if (!enumerator.MoveNext())
            return;

        await enumerator.Current.Invoke(request, response,
            async () => await InvokeMiddlewareChain(request, response, enumerator));
    }

    public void Use(Func<IRequest, IResponse, Func<Task>, Task> action) => _middlewares.Add(action);

    public void Use(Func<IRequest, IResponse, Task> action) =>
        _middlewares.Add(async (request, response, next) =>
        {
            await action.Invoke(request, response);
            await next();
        });

    public void Use(string path, Func<IRequest, IResponse, Task> action) =>
        _routes.Add(new KeyValuePair<string, Func<IRequest, IResponse, Task>>(path, action));

    public void UseStaticFiles(string basePath)
    {
        _middlewares.Add(async (request, response, next) =>
        {
            if (request.Path.StartsWith('/' + basePath))
            {
                var filePath = Path.Combine(Environment.CurrentDirectory, request.Path[1..]);
                if (File.Exists(filePath) && !request.Path.Contains(".."))
                {
                    var fileBuffer = await File.ReadAllBytesAsync(filePath, _cancellationToken);
                    await response.SendFile(fileBuffer, Path.GetFileName(filePath));
                }
                else
                    await response.StatusCode(EStatusCode.NotFound);
            }
            else
                await next();
        });
    }
}