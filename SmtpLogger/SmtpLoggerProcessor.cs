using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Mail;
using System.Threading;

namespace SmtpLogger
{
    internal class SmtpLoggerProcessor : IDisposable
    {
        private readonly SmtpLoggerOptions _options;
        private readonly Queue<LogMessageEntry> _messageQueue;
        private bool _isAddingCompleted;
        private int _maxQueuedMessages = SmtpLoggerOptions.DefaultMaxQueueLengthValue;
        public int MaxQueueLength
        {
            get => _maxQueuedMessages;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                lock (_messageQueue)
                {
                    _maxQueuedMessages = value;
                    Monitor.PulseAll(_messageQueue);
                }
            }
        }
        private readonly Thread _outputThread;

        public SmtpLoggerProcessor(SmtpLoggerOptions options)
        {
            _options = options;
            _messageQueue = new Queue<LogMessageEntry>();
            MaxQueueLength = options.MaxQueueLength ?? SmtpLoggerOptions.DefaultMaxQueueLengthValue;

            _outputThread = new Thread(ProcessLogQueue)
            {
                IsBackground = true,
                Name = "Smtp logger queue processing thread"
            };
            _outputThread.Start();
        }

        internal void WriteMessage(SmtpClient client, LogMessageEntry entry)
        {
            try
            {
                var parts = new string?[] { _options.ServiceName, entry.CategoryName }
                    .Where(x => x != null);

                var subject = $"[{entry.LogLevel}] {string.Join(" - ", parts)}".Trim();
                var body = entry.Message;
                var mail = new MailMessage(_options.From, _options.To, subject, body)
                {
                    IsBodyHtml = true
                };

                client.Send(mail);
            }
            catch
            {
                CompleteAdding();
            }
        }

        private void ProcessLogQueue()
        {
            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                Credentials = new NetworkCredential(_options.Username, _options.Password),
                EnableSsl = _options.EnableSsl
            };

            while (TryDequeue(out LogMessageEntry message))
            {
                WriteMessage(client, message);
            }
        }

        public bool Enqueue(LogMessageEntry item)
        {
            lock (_messageQueue)
            {
                while (_messageQueue.Count >= MaxQueueLength && !_isAddingCompleted)
                {

                    Monitor.Wait(_messageQueue);
                }

                if (!_isAddingCompleted)
                {
                    Debug.Assert(_messageQueue.Count < MaxQueueLength);
                    bool startedEmpty = _messageQueue.Count == 0;

                    _messageQueue.Enqueue(item);

                    if (startedEmpty)
                    {
                        Monitor.PulseAll(_messageQueue);
                    }

                    return true;
                }
            }

            return false;
        }

        public bool TryDequeue(out LogMessageEntry item)
        {
            lock (_messageQueue)
            {
                while (_messageQueue.Count == 0 && !_isAddingCompleted)
                {
                    Monitor.Wait(_messageQueue);
                }

                if (_messageQueue.Count > 0)
                {
                    item = _messageQueue.Dequeue();
                    if (_messageQueue.Count == MaxQueueLength - 1)
                    {
                        Monitor.PulseAll(_messageQueue);
                    }

                    return true;
                }

                item = default;
                return false;
            }
        }

        public void Dispose()
        {
            CompleteAdding();

            try
            {
                _outputThread.Join(1500);
            }
            catch (ThreadStateException) { }
        }

        private void CompleteAdding()
        {
            lock (_messageQueue)
            {
                _isAddingCompleted = true;
                Monitor.PulseAll(_messageQueue);
            }
        }
    }
}
