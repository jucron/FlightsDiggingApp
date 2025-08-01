

using System.Text.Json.Serialization;
using FlightsDiggingApp.Properties;
using FlightsDiggingApp.Services;
using FlightsDiggingApp.Services.Amadeus;
using FlightsDiggingApp.Services.Auth;
using FlightsDiggingApp.Services.Filters;
using FlightsDiggingApp.Utils;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace FlightsDiggingApp
{
    
    public class BuilderHelper
    {
        private static EnvironmentProperties? _environmentProperties;

        public static readonly string CORS_POLICY_ALLOW_ALL = "AllowAll";
        public static readonly string CORS_POLICY_ALLOW_FRONT = "AllowFront";
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
            var env = GetEnvironmentVariable(builder);
            Base64BuilderHelper.CreateApiPropertiesFile(env);

            // Populating Properties
            builder.Configuration
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "api_properties_values.json"), optional: false, reloadOnChange: true);

            builder.Services
                .Configure<AmadeusApiProperties>(builder.Configuration.GetSection("AmadeusApiValues"))
                .Configure<AffiliateProperties>(builder.Configuration.GetSection("AffiliateProperties"));
                
        }

        internal static void AddSingletonsDependencies(WebApplicationBuilder builder)
        {
            // Registering Dependency Injection
            builder.Services.AddSingleton<IPropertiesProvider, PropertiesProvider>();
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<ICacheService, CacheService>();
            builder.Services.AddSingleton<IAmadeusAuthService, AmadeusAuthService>();
            builder.Services.AddSingleton<IRoundTripApiService, AmadeusApiService>();
            builder.Services.AddSingleton<IFlightsDiggerService, FlightsDiggerService>();
            builder.Services.AddSingleton<IFilterService, FilterService>();
            builder.Services.AddSingleton<IAuthService, AuthService>();
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

        internal static void SetupCors(WebApplicationBuilder builder)
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
                    policy.WithOrigins(GetEnvironmentVariable(builder).FRONT_URL ?? "missing_front_address")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });
        }

        /// <summary>
        /// TEMP service provider to populate EnvironmentProperties early. Use it only after running AddEnvironmentProperties().
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        internal static EnvironmentProperties GetEnvironmentVariable(WebApplicationBuilder builder)
        {
            if (_environmentProperties == null)
            {
                var tempProvider = builder.Services.BuildServiceProvider();
                _environmentProperties = tempProvider.GetRequiredService<IOptions<EnvironmentProperties>>().Value;
            }
            return _environmentProperties;
        }

        internal static void SetupPort(WebApplicationBuilder builder)
        {
            var port = Environment.GetEnvironmentVariable("PORT"); //Defined by the Server
            
            if (port != null)
            {
                Console.WriteLine($"PORT found in Environment with value: {port}");
                builder.WebHost.UseUrls($"http://*:{port}");
            }
        }

        internal static void AddEnvironmentProperties(WebApplicationBuilder builder)
        {
            builder.Configuration.AddEnvironmentVariables();
            builder.Services
                .Configure<EnvironmentProperties>(builder.Configuration.GetSection("EnvironmentProperties"));
        }
    }
}
