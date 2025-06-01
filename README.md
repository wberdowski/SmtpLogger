# SmtpLogger

SmtpLogger is a simple email logger that allows you to log messages to an SMTP server.
It's compatible with Microsoft.Extensions.Logging

[![Nuget](https://img.shields.io/nuget/v/SmtpLogger)](https://www.nuget.org/packages/SmtpLogger)

## How to use

1. Install the SmtpLogger package via NuGet:
```
dotnet add package SmtpLogger
```

2. Configure the logger in your application
```csharp
using SmtpLogger;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSmtpLogger();
```
