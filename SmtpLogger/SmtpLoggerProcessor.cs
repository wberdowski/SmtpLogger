using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Threading;

namespace SmtpLogger
{
    internal class SmtpLoggerProcessor : IDisposable
    {
        private readonly SmtpLoggerOptions _options;
        private readonly Queue<LogMessageEntry> _messageQueue;
        private volatile int _messagesDropped;
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
            // Start Console message queue processor
            _outputThread = new Thread(ProcessLogQueue)
            {
                IsBackground = true,
                Name = "Smtp logger queue processing thread"
            };
            _outputThread.Start();
        }

        // internal for testing
        internal void WriteMessage(SmtpClient client, LogMessageEntry entry)
        {
            try
            {
                var subject = $"[{entry.LogLevel}] {entry.CategoryName}";
                var body = entry.Message;
                var mail = new MailMessage(_options.From, _options.To, subject, body);

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

                    // if we just logged the dropped message warning this could push the queue size to
                    // MaxLength + 1, that shouldn't be a problem. It won't grow any further until it is less than
                    // MaxLength once again.
                    _messageQueue.Enqueue(item);

                    // if the queue started empty it could be at 1 or 2 now
                    if (startedEmpty)
                    {
                        // pulse for wait in Dequeue
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
                        // pulse for wait in Enqueue
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
                _outputThread.Join(1500); // with timeout in-case Console is locked by user input
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
