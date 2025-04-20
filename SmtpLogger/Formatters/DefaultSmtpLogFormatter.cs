using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;

namespace SmtpLogger.Formatters
{
    internal sealed class DefaultSmtpLogFormatter : SmtpLogFormatter, IDisposable
    {
        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
        {
            var accentColor = logEntry.LogLevel switch
            {
                LogLevel.Critical => "#d50000",
                LogLevel.Error => "#a32921",
                LogLevel.Warning => "#735308",
                LogLevel.Information => "#06677d",
                LogLevel.Debug => "#474747",
                LogLevel.Trace => "#474747",
                _ => "#474747"
            };

            textWriter.WriteLine("<div>");
            textWriter.WriteLine($"<span style=\"display: inline-block; height: 2rem; width: 0.5rem; background-color: {accentColor}; vertical-align: middle;\"></span>");
            textWriter.WriteLine($"<span style=\"font-family: sans-serif; padding: 0.5rem;\">{logEntry.Formatter(logEntry.State, logEntry.Exception)}</span>");
            textWriter.WriteLine("</div>");

            if (logEntry.Exception != null)
            {
                textWriter.WriteLine($"<pre style=\"border: 1px solid {accentColor}; padding: 1rem;\">");
                textWriter.WriteLine(logEntry.Exception.ToString());
                textWriter.WriteLine("</pre>");
            }
        }
        public void Dispose()
        {

        }

    }
}
