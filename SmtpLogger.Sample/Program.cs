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
var logger = app.Services.GetRequiredService<ILogger<System.Security.Cryptography.X509Certificates.CertificateRequest>>();

try
{
    uint.Parse("-1");
}
catch (Exception ex)
{
    logger.LogCritical(ex, "A test CRITICAL exception Id={Id}", 1);
    logger.LogError(ex, "A test exception Id={Id}", 2);
    logger.LogWarning(ex, "A test warning exception Id={Id}", 3);
    logger.LogDebug(ex, "A test debug exception Id={Id}", 4);
    logger.LogTrace(ex, "A test trace exception Id={Id}", 5);
}

app.Run();