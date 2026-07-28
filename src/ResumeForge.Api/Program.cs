var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/api/status", () =>
{
    return Results.Ok(new
    {
        message = "A API do Currículo Pronto está funcionando."
    });
});

app.Run();
