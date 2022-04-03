using System.Text.RegularExpressions;

namespace Caruti.Http;

public class WebApplication : IWebApplication
{
    private IApplicationServer Server { get; }

    //TODO: Change to a IEnumerable<Route>
    private readonly IDictionary<HttpMethod, IDictionary<string, Func<IRequest, IResponse, Task>>> _routes;

    private readonly ICollection<Func<IRequest, IResponse, Func<Task>, Task>> _middlewares;

    //TODO: move to a RouteHandler
    public static readonly Regex PathWithParamRegex = new Regex("{(.)*}", RegexOptions.Compiled);

    public WebApplication(IApplicationServer server)
    {
        Server = server;
        Server.OnReceiveConnection = ReceiveConnection;
        _middlewares = new List<Func<IRequest, IResponse, Func<Task>, Task>>();
        _routes = new Dictionary<HttpMethod, IDictionary<string, Func<IRequest, IResponse, Task>>>();
    }

    public Task Listen(CancellationToken cancellationToken = default)
    {
        Use(async (request, response) =>
        {
            var key = MatchRoute(request.Method, request.Path, _routes[request.Method].Keys);
            if (key != null)
            {
                var route = _routes[request.Method][key];
                request.SetParams(key);
                await route.Invoke(request, response);
            }
            else
                await response.StatusCode(EStatusCode.NotFound);
        });

        return Server.Listen(cancellationToken);
    }

    //TODO: move route matching and route especific things to a RouteHandler and Route classes 
    private string? MatchRoute(HttpMethod method, string path, IEnumerable<string> keys)
    {
        if (_routes[method].ContainsKey(path))
            return path;

        var pathsWithParams = keys.Where(x => PathWithParamRegex.IsMatch(x));
        var matchRegex = GetPathMatchRegex(path);
        return pathsWithParams.SingleOrDefault(x => matchRegex.IsMatch(x));
    }

    private static Regex GetPathMatchRegex(string path)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var patternBuilder = new StringBuilder();
        const string paramPattern = "|/{(.)*})";
        foreach (var part in parts)
        {
            patternBuilder.Append('(');
            patternBuilder.Append('/');
            patternBuilder.Append(part);
            patternBuilder.Append(paramPattern);
        }

        return new Regex(patternBuilder.ToString());
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

            //TODO: Change how server reacts to Connection header
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

    public void Use(HttpMethod method, string path, Func<IRequest, IResponse, Task> action)
    {
        if (!_routes.ContainsKey(method))
            _routes.Add(
                new KeyValuePair<HttpMethod, IDictionary<string, Func<IRequest, IResponse, Task>>>(method,
                    new Dictionary<string, Func<IRequest, IResponse, Task>>()));

        _routes[method].Add(new KeyValuePair<string, Func<IRequest, IResponse, Task>>(path, action));
    }

    public void UseStaticFiles(string basePath)
    {
        _middlewares.Add(async (request, response, next) =>
        {
            if (request.Path.StartsWith('/' + basePath))
            {
                var filePath = Path.Combine(Environment.CurrentDirectory, request.Path[1..]);
                if (File.Exists(filePath) && !request.Path.Contains(".."))
                {
                    var fileBuffer = await File.ReadAllBytesAsync(filePath);
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