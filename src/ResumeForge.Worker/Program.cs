using CurriculoPronto.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddHostedService<WorkerService>();

var host = builder.Build();
host.Run();
