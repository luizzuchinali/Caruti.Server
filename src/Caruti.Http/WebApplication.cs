namespace Caruti.Http;

public class WebApplication : IWebApplication
{
    private IApplicationServer Server { get; }

    private readonly IDictionary<string, Func<IRequest, IResponse, Task>> _routes;

    private readonly ICollection<Func<IRequest, IResponse, Func<Task>, Task>> _middlewares;

    public WebApplication(IApplicationServer server)
    {
        Server = server;
        Server.OnReceiveConnection = ReceiveConnection;
        _middlewares = new List<Func<IRequest, IResponse, Func<Task>, Task>>();
        _routes = new Dictionary<string, Func<IRequest, IResponse, Task>>();
    }

    public Task Listen(CancellationToken cancellationToken = default)
    {
        Use(async (request, response) =>
        {
            if (!_routes.ContainsKey(request.Path))
            {
                await response.StatusCode(EStatusCode.NotFound);
            }
            else
            {
                var route = _routes[request.Path];
                await route.Invoke(request, response);
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
}