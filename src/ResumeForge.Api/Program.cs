var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/api/status", () =>
{
    return Results.Ok(new
    {
        message = "A API do Currículo Pronto está funcionando."
    });
});

app.Run();
