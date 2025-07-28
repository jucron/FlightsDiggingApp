using FlightsDiggingApp.Properties;
using FlightsDiggingApp;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

BuilderHelper.AddControllers(builder);

#if DEBUG
BuilderHelper.AddSwagger(builder);
#endif

BuilderHelper.AddPropertiesDependencies(builder);

BuilderHelper.AddSingletonsDependencies(builder);

BuilderHelper.ConfigureLogger(builder);

// TEMP service provider to resolve EnvironmentProperties early
var tempProvider = builder.Services.BuildServiceProvider();
var envProps = tempProvider.GetRequiredService<IPropertiesProvider>().EnvironmentProperties;

BuilderHelper.SetupCors(builder, envProps);

// Build App
var app = builder.Build();

#if DEBUG
// Dev tools
app.UseDeveloperExceptionPage();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();
#endif

app.UseRouting();              // Enable routing for controllers

#if DEBUG
app.UseCors(BuilderHelper.CORS_POLICY_ALLOW_ALL);
#elif RELEASE
app.UseCors(BuilderHelper.CORS_POLICY_ALLOW_FRONT);       
#endif

app.UseWebSockets();           // Enable WebSocket support before routing

app.UseHttpsRedirection();     // Redirect HTTP to HTTPS (optional)

app.UseAuthorization();        // Optional: ApplyFilter Authorization (if necessary)

app.MapControllers();           // Map controllers

app.Run();
