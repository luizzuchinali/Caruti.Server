namespace Caruti.Http;

public class WebApplication : IWebApplication
{
    private IApplicationServer Server { get; }

    private readonly ICollection<Func<IRequest, IResponse, Func<Task>, Task>> _middlewares;

    public WebApplication(IApplicationServer server)
    {
        Server = server;
        Server.OnReceiveConnection = ReceiveConnection;
        _middlewares = new List<Func<IRequest, IResponse, Func<Task>, Task>>();
    }

    public Task Listen(CancellationToken cancellationToken = default)
    {
        return Server.Listen(cancellationToken);
    }

    private async Task ReceiveConnection(IConnection connection, CancellationToken cancellationToken)
    {
        var stream = connection.Stream;
        while (connection.Connected && !cancellationToken.IsCancellationRequested)
        {
            //TODO: add configuration capability to change default request size
            var buffer = new byte[1024 * 8];
            var bufferIndex = 0;
            while (stream.Socket.Available != 0)
            {
                buffer[bufferIndex] = (byte)stream.ReadByte();
                bufferIndex++;
            }

            if (bufferIndex == 0)
                continue;

            var readOnlyBuffer = new ReadOnlyMemory<byte>(buffer);

            //TODO: share between request/response protocol informations
            var request = new Request(readOnlyBuffer);
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
                connection.Close();
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
}