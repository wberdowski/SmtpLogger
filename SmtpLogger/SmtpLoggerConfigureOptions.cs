using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace SmtpLogger
{
    internal sealed class SmtpLoggerConfigureOptions : IConfigureOptions<SmtpLoggerOptions>
    {
        private readonly IConfiguration _configuration;

        public SmtpLoggerConfigureOptions(ILoggerProviderConfiguration<SmtpLoggerProvider> providerConfiguration)
        {
            _configuration = providerConfiguration.Configuration;
        }

        public void Configure(SmtpLoggerOptions options) => _configuration.Bind(options);
    }
}
