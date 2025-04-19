using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmtpLogger;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddSmtpLogger();

var app = builder.Build();

// Use the logger
var logger = app.Services.GetRequiredService<ILogger<Program>>();

try
{
    uint.Parse("-1");
}
catch (Exception ex)
{
    logger.LogError(ex, "An error occurred while processing your request.");
}

app.Run();