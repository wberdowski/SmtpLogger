using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

            try
            {
                _outputThread = new Thread(ProcessLogQueue)
                {
                    IsBackground = true,
                    Name = "Smtp logger queue processing thread"
                };
                _outputThread.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting SMTP logger thread: {ex.Message}");
            }
        }

        internal void WriteMessage(SmtpClient client, LogMessageEntry entry)
        {
            try
            {
                var parts = new string?[] { _options.ServiceName, entry.CategoryName }
                    .Where(x => x != null);

                var mail = new MimeMessage();
                mail.From.Add(new MailboxAddress(string.Empty, _options.From));
                mail.To.Add(new MailboxAddress(string.Empty, _options.To));
                mail.Subject = $"[{entry.LogLevel}] {string.Join(" - ", parts)}".Trim();

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = entry.Message
                };

                mail.Body = bodyBuilder.ToMessageBody();

                client.Send(mail);
            }
            catch
            {
                CompleteAdding();
            }
        }

        private void ProcessLogQueue()
        {
            using var client = new SmtpClient();

            client.Connect(_options.Host, _options.Port, _options.EnableSsl);

            if (_options.Username != string.Empty || _options.Password != string.Empty)
                client.Authenticate(_options.Username, _options.Password);

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
