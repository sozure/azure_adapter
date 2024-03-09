namespace VGManager.Adapter.Api;

static partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var specificOrigins = Constants.Cors.AllowSpecificOrigins;

        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole();

        ConfigureServices(builder, specificOrigins);

        var app = builder.Build();

        Configure(app, specificOrigins);
        await app.RunAsync();
    }
}


