var server = new TcpApplicationServer("0.0.0.0")
{
    OnListen = () => Console.WriteLine("Listening on: http://localhost:8080")
};

var app = new WebApplication(server);

app.UseStaticFiles("public");

app.Use("/users", async (_, response) =>
{
    const string html = "<h1>Hello from Caruti</h1>";
    await response.SendHtml(html, EStatusCode.Ok);
});

await app.Listen();