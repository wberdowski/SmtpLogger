using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO;

namespace SmtpLogger
{
    public abstract class SmtpLogFormatter
    {
        protected SmtpLogFormatter()
        {
        }

        public abstract void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter);
    }
}
