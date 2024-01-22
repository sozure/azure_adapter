using VGManager.Adapter.Api;

var builder = WebApplication.CreateBuilder(args);

var specificOrigins = Constants.Cors.AllowSpecificOrigins;

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole();

ConfigureServices(builder, specificOrigins);

var app = builder.Build();

await ConfigureAsync(app, specificOrigins);
await app.RunAsync();
