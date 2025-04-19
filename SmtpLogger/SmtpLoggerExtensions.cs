using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using System;

namespace SmtpLogger
{
    public static class SmtpLoggerExtensions
    {

        public static ILoggingBuilder AddSmtpLogger(this ILoggingBuilder builder)
        {
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SmtpLoggerProvider>());

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<SmtpLoggerOptions>, SmtpLoggerConfigureOptions>());

            return builder;
        }

        public static ILoggingBuilder AddSmtpLogger(this ILoggingBuilder builder, Action<SmtpLoggerOptions> configure)
        {
            builder.AddSmtpLogger();
            builder.Services.Configure(configure);

            return builder;
        }
    }

}
