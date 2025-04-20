using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Linq;

namespace SmtpLogger
{
    public sealed class SmtpLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly SmtpLoggerProcessor _processor;
        private readonly SmtpLogFormatter _formatter;
        private static StringWriter? _writer;

        internal SmtpLogger(string categoryName, SmtpLoggerProcessor processor, SmtpLogFormatter formatter)
        {
            _categoryName = categoryName;
            _processor = processor;
            _formatter = formatter;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId,
            TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            _writer ??= new StringWriter();

            LogEntry<TState> logEntry = new LogEntry<TState>(logLevel, _categoryName, eventId, state, exception, formatter);
            _formatter.Write(in logEntry, null, _writer);

            var sb = _writer.GetStringBuilder();

            if (sb.Length == 0)
            {
                return;
            }

            string computedAnsiString = sb.ToString();
            sb.Clear();

            if (sb.Capacity > 1024)
            {
                sb.Capacity = 1024;
            }

            _processor.Enqueue(new LogMessageEntry(logLevel, _categoryName, computedAnsiString));
        }
    }

}
