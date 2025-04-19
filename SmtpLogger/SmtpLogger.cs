using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace SmtpLogger
{
    public sealed class SmtpLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly SmtpLoggerProcessor _processor;

        internal SmtpLogger(string categoryName, SmtpLoggerProcessor processor)
        {
            _categoryName = categoryName;
            _processor = processor;
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

            var sb = new StringBuilder(formatter(state, exception));

            if (exception != null)
            {
                sb.AppendLine("\r\n" + exception.ToString());
            }

            _processor.Enqueue(new LogMessageEntry(logLevel, _categoryName, sb.ToString()));
        }
    }

}
