using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SmtpLogger
{
    [ProviderAlias("SmtpLogger")]
    public sealed class SmtpLoggerProvider : ILoggerProvider
    {
        private readonly SmtpLoggerProcessor _processor;

        public SmtpLoggerProvider(IOptionsMonitor<SmtpLoggerOptions> options)
        {
            _processor = new SmtpLoggerProcessor(
                options.CurrentValue
            );
        }

        public ILogger CreateLogger(string categoryName) => new SmtpLogger(
            categoryName,
            _processor);

        public void Dispose() { }
    }

}
