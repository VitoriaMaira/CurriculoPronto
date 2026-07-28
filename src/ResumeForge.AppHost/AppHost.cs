var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.CurriculoPronto_Api>("api");
builder.AddProject<Projects.CurriculoPronto_Worker>("worker");

builder.Build().Run();
