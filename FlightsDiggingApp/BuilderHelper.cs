

using System.Text.Json.Serialization;
using FlightsDiggingApp.Properties;
using FlightsDiggingApp.Services;
using FlightsDiggingApp.Services.Filters;
using Microsoft.Extensions.Logging.Console;

namespace FlightsDiggingApp
{
    public class BuilderHelper
    {
        public static string CORS_POLICY_ALLOW_ALL = "AllowAll";
        public static string CORS_POLICY_ALLOW_FRONT = "AllowFront";
        internal static void AddControllers(WebApplicationBuilder builder)
        {
            // Add controllers to the container.
            builder.Services.AddControllers()
                .AddJsonOptions(opt =>
                    {
                        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    });
        }
        internal static void AddPropertiesDependencies(WebApplicationBuilder builder)
        {
            // Populating Properties
            builder.Configuration
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("api_properties_values.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            builder.Services
                .Configure<AmadeusApiProperties>(builder.Configuration.GetSection("AmadeusApiValues"))
                .Configure<AffiliateProperties>(builder.Configuration.GetSection("AffiliateProperties"))
                .Configure<EnvironmentProperties>(builder.Configuration.GetSection("EnvironmentProperties"));
        }

        internal static void AddSingletonsDependencies(WebApplicationBuilder builder)
        {
            // Registering Dependency Injection
            builder.Services.AddSingleton<IPropertiesProvider, PropertiesProvider>();
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<ICacheService, CacheService>();
            builder.Services.AddSingleton<IAuthService, AmadeusAuthService>();
            builder.Services.AddSingleton<IApiService, AmadeusApiService>();
            builder.Services.AddSingleton<IFlightsDiggerService, FlightsDiggerService>();
            builder.Services.AddSingleton<IFilterService, FilterService>();
        }

        internal static void AddSwagger(WebApplicationBuilder builder)
        {
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                var baseUrl = builder.Configuration["Swagger:BaseUrl"];
                if (!string.IsNullOrEmpty(baseUrl))
                {
                    c.AddServer(new Microsoft.OpenApi.Models.OpenApiServer { Url = baseUrl });
                }
            });
        }

        internal static void ConfigureLogger(WebApplicationBuilder builder)
        {
            // Configure logging using ConsoleFormatterOptions
            builder.Logging.ClearProviders(); // Remove default providers
            builder.Logging.AddConsole(options =>
            {
                options.FormatterName = ConsoleFormatterNames.Simple; // Use "Simple" format
            });

            builder.Services.Configure<SimpleConsoleFormatterOptions>(options =>
            {
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss "; // Set timestamp format
                options.IncludeScopes = true; // Optional: Show log scopes
            });
        }

        internal static void SetupCors(WebApplicationBuilder builder, EnvironmentProperties envProp)
        {
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(CORS_POLICY_ALLOW_ALL, policy =>
                {
                    // Allow any origin (less secure, use only in development):
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
                options.AddPolicy(CORS_POLICY_ALLOW_FRONT, policy =>
                {
                    policy.WithOrigins(envProp.front_url)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });
        }
    }
}
