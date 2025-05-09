using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SmtpLogger.Sample;
internal sealed class CustomSmtpLogFormatter : SmtpLogFormatter
{
    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        textWriter.WriteLine($"[{logEntry.LogLevel}] {logEntry.Category} {logEntry.Exception?.ToString()}");
    }
}
