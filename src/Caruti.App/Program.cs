var server = new TcpApplicationServer("0.0.0.0")
{
    OnListen = () => Console.WriteLine("Listening on: http://localhost:8080")
};

var app = new WebApplication(server);

app.Use(async (request, _, next) =>
{
    WriteLine(request.Protocol);
    WriteLine(request.Method);
    WriteLine(request.Path);
    WriteLine(request.Query);
    await next();
});

app.Use(async (_, response) =>
{
    WriteLine("--- middleware 2 ---");
    const string html = "<h1>Hello from Caruti</h1>";
    await response.SendHtml(html, 200);
});

await app.Listen();