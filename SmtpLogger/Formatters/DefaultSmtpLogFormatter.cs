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
                LogLevel.Critical => "#ff2121",
                LogLevel.Error => "#ff2121",
                LogLevel.Warning => "#ffbd21",
                LogLevel.Information => "#3baf43",
                LogLevel.Debug => "#21c4ff",
                LogLevel.Trace => "#474747",
                _ => "#474747"
            };

            textWriter.WriteLine("<table style=\"width: 100%;\">");
            textWriter.WriteLine("<tr>");
            textWriter.WriteLine($"<td style=\"width: 0.125rem; background-color: {accentColor}; vertical-align: middle;\"></td>");
            textWriter.WriteLine($"<td style=\"padding: 0.25rem; font-family: sans-serif; font-weight: bold; background-color: #eee;\">" +
                $"<div style=\"padding: 0.25rem;\"><small>{logEntry.Category}</small></div>" +
                $"<div style=\"padding: 0.25rem;\">{logEntry.Formatter(logEntry.State, logEntry.Exception)}</div>" +
            $"</td>");
            textWriter.WriteLine("</tr>");

            if (logEntry.Exception != null)
            {
                textWriter.WriteLine("<tr>");
                textWriter.WriteLine($"<td style=\"background-color: {accentColor}; vertical-align: middle;\"></td>");
                textWriter.WriteLine($"<td style=\"padding: 0.5rem; font-family: sans-serif; font-size: 0.825rem;\">" +
                    $"<pre>{logEntry.Exception.ToString()}</pre>" +
                    $"</td>");
                textWriter.WriteLine("</tr>");
            }

            textWriter.WriteLine("</table>");
        }
        public void Dispose()
        {

        }

    }
}
