using FlightsDiggingApp;
using FlightsDiggingApp.Properties;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

BuilderHelper.SetupPort(builder);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

Console.WriteLine("Starting FlightsDiggingApp...");

BuilderHelper.AddControllers(builder);

BuilderHelper.AddEnvironmentProperties(builder);

#if DEBUG
BuilderHelper.AddSwagger(builder);
#endif

BuilderHelper.AddPropertiesDependencies(builder);

BuilderHelper.AddSingletonsDependencies(builder);

BuilderHelper.ConfigureLogger(builder);

BuilderHelper.SetupCors(builder);

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
