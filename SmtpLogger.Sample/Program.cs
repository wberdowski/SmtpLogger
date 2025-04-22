using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmtpLogger;
using SmtpLogger.Sample;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddSmtpLogger();

builder.Services.AddSingleton<DemoService>();

var app = builder.Build();

var demoService = app.Services.GetRequiredService<DemoService>();
demoService.DoSomethingAndThrow();

app.Run();