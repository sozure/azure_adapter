using Microsoft.OpenApi.Models;
using System.Reflection;
using VGManager.Adapter.Api;
using VGManager.Adapter.Api.HealthChecks;
using VGManager.Adapter.Azure;
using VGManager.Adapter.Azure.Helper;
using VGManager.Adapter.Azure.Interfaces;
using VGManager.Adapter.Kafka.Extensions;
using VGManager.Adapter.Models.Kafka;

static partial class Program
{
    public static WebApplicationBuilder ConfigureServices(WebApplicationBuilder self, string specificOrigins)
    {
        var configuration = self.Configuration;
        var services = self.Services;

        services.AddCors(options =>
        {
            options.AddPolicy(name: specificOrigins,
                                policy =>
                                {
                                    policy.WithOrigins("http://localhost:3000")
                                    .AllowAnyMethod()
                                    .AllowAnyHeader();
                                });
        });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "VGManager.Adapter.Api", Version = "v1" });
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);
            c.UseOneOfForPolymorphism();
        });

        services.AddAuthorization();
        services.AddControllers();
        services.AddHealthChecks()
            .AddCheck<StartupHealthCheck>(nameof(StartupHealthCheck), tags: new[] { "startup" });

        services.AddAutoMapper(
            typeof(Program)
        );

        RegisterServices(services, configuration);

        return self;
    }

    private static void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<StartupHealthCheck>();

        services.AddScoped<IVariableGroupAdapter, VariableGroupAdapter>();
        services.AddScoped<IProjectAdapter, ProjectAdapter>();
        services.AddScoped<IKeyVaultAdapter, KeyVaultAdapter>();
        services.AddScoped<IHttpClientProvider, HttpClientProvider>();
        services.AddScoped<IProfileAdapter, ProfileAdapter>();
        services.AddScoped<IGitRepositoryAdapter, GitRepositoryAdapter>();
        services.AddScoped<IGitVersionAdapter, GitVersionAdapter>();
        services.AddScoped<IGitFileAdapter, GitFileAdapter>();
        services.AddScoped<IReleasePipelineAdapter, ReleasePipelineAdapter>();
        services.AddScoped<IBuildPipelineAdapter, BuildPipelineAdapter>();
        services.AddScoped<ISprintAdapter, SprintAdapter>();

        services.SetupKafkaConsumer<VGManagerAdapterCommand>(configuration, Constants.SettingKeys.AdapterCommandResponseConsumer, false);
        services.SetupKafkaProducer<VGManagerAdapterCommandResponse>(configuration, Constants.SettingKeys.VGManagerAdapterCommandResponseProducer);
    }
}
