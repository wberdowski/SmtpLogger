# SmtpLogger 
[![Nuget](https://img.shields.io/nuget/v/SmtpLogger)](https://www.nuget.org/packages/SmtpLogger)
https://www.nuget.org/packages/SmtpLogger


> [!WARNING] 
> This package is in pre-release state and may change in future versions.


SmtpLogger is a simple email logger that allows you to log messages to an SMTP server.
It's compatible with Microsoft.Extensions.Logging

## How to install

You can install SmtpLogger via NuGet package manager:

```bash
dotnet add package SmtpLogger --prerelease
```

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

## Configuration

- In appsettings.json

```json
"Logging": {
    "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore": "Warning"
    },
    "SmtpLogger": {
        "LogLevel": {
            "Default": "Error"
        },
        "Host": "127.0.0.1",
        "Port": 1025,
        "From": "no-reply@myapplication.com",
        "To": "admin@myapplication.com",
        "ServiceName": "My application"
    }
}
```

or

- In code
```csharp
x.AddSmtpLogger(c =>
{
    c.Host = "127.0.0.1";
    c.Port = 1025;
    c.From = "no-reply@myapplication.com";
    c.To = "admin@myapplication.com";
    c.ServiceName = "My application";
});
```