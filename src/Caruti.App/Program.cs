var server = new TcpApplicationServer("0.0.0.0")
{
    OnListen = () => Console.WriteLine("Listening on: http://localhost:8080")
};

var app = new WebApplication(server);

app.UseStaticFiles("public");

app.Get("/user", async (_, response) =>
{
    const string html = "<h1>Hello from Caruti</h1>";
    await response.SendHtml(html, EStatusCode.Ok);
});

app.Get("/user/fixed", async (request, response) =>
{
    const string html = "<h1>Hello from fixed route Caruti</h1>";
    await response.SendHtml(html, EStatusCode.Ok);
});

app.Get("/user/{id}", async (request, response) =>
{
    var id = request.GetParam<Guid>("id");
    var html = $"<h1>Hello from param Caruti {id}</h1>";
    await response.SendHtml(html, EStatusCode.Ok);
});

app.Get("/user/{id}/address/{number}", async (request, response) =>
{
    var id = request.GetParam<Guid>("id");
    var number = request.GetParam<int>("number");
    var html = $"<h1>Hello from param Caruti {id} {number}</h1>";
    await response.SendHtml(html, EStatusCode.Ok);
});

app.Post("/user", async (request, response) =>
{
    var data = await request.FromJson<User>();
    WriteLine(data.Name);
    await response.StatusCode(EStatusCode.Ok);
});


await app.Listen();