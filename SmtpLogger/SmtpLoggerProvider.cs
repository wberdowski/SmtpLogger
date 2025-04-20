using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SmtpLogger
{
    [ProviderAlias("SmtpLogger")]
    public sealed class SmtpLoggerProvider : ILoggerProvider
    {
        private readonly SmtpLoggerProcessor _processor;
        private readonly SmtpLogFormatter _formatter;

        public SmtpLoggerProvider(IOptionsMonitor<SmtpLoggerOptions> options, SmtpLogFormatter formatter)
        {
            _processor = new SmtpLoggerProcessor(
                options.CurrentValue
            );
            _formatter = formatter;
        }

        public ILogger CreateLogger(string categoryName) => new SmtpLogger(
            categoryName,
            _processor,
            _formatter);

        public void Dispose() { }
    }

}
