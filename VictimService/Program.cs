using Prometheus;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseHttpMetrics();

app.MapGet("/", () => "Hello World!");

app.MapMetrics();

app.Run();