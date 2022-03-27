var server = new TcpApplicationServer("0.0.0.0")
{
    OnListen = () => Console.WriteLine("Listening on: http://localhost:8080")
};

var app = new WebApplication(server);

await app.Listen();