# Caruti.Server

- Basic WebApplication

```C#
var server = new TcpApplicationServer("0.0.0.0")
{
    OnListen = () => Console.WriteLine("Listening on: http://localhost:8080")
};

var app = new WebApplication(server);

app.UseStaticFiles("public");

app.Get("/hello", async (request, response) =>
{
    const string html = "<h1>Hello from Caruti</h1>";
    await response.SendHtml(html, EStatusCode.Ok);
});

await app.Listen();
```

- This project is experimental, use at your own risk!
