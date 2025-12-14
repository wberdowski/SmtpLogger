using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmtpLogger
{
    internal class SmtpLoggerProcessor : IDisposable
    {
        private const int MaxConnectAttempts = 3;
        private const int BaseDelayBetweenConnectAttempts = 1000;
        private readonly SmtpLoggerOptions _options;
        private readonly Queue<LogMessageEntry> _messageQueue;
        private int _maxQueuedMessages = SmtpLoggerOptions.DefaultMaxQueueLengthValue;
        private SmtpClient _client;
        private bool _isConnected;

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

        // Batch fields
        private readonly List<LogMessageEntry> _batch = new List<LogMessageEntry>();
        private Timer _batchTimer;
        private readonly object _batchLock = new object();

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

        internal void WriteMessage(LogMessageEntry entry)
        {
            // This method is now used for batch sending only
            if (_client == null || !_isConnected)
            {
                ConnectWithRetry();
            }

            if (!_isConnected)
            {
                Debug.WriteLine("Failed to connect to SMTP server. Message will not be sent.");
                return;
            }

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

                _client?.Send(mail);
                _isConnected = true;
                CompleteAdding();
            }
            catch (Exception)
            {
                _isConnected = false;
            }
        }

        public bool Enqueue(LogMessageEntry item)
        {
            lock (_messageQueue)
            {
                while (_messageQueue.Count >= MaxQueueLength)
                {
                    Monitor.Wait(_messageQueue);
                }

                Debug.Assert(_messageQueue.Count < MaxQueueLength);
                bool startedEmpty = _messageQueue.Count == 0;

                _messageQueue.Enqueue(item);
                Debug.WriteLine($"[SmtpLogger] Dodano wiadomość do kolejki. Liczba wpisów w kolejce (po dodaniu): {_messageQueue.Count}");

                if (startedEmpty)
                {
                    Monitor.PulseAll(_messageQueue);
                }

                return true;
            }

            return false;
        }

        private void WriteBatch(List<LogMessageEntry> batch)
        {
            if (batch.Count == 0)
                return;

            if (_client == null || !_isConnected)
            {
                ConnectWithRetry();
            }

            if (!_isConnected)
            {
                Debug.WriteLine("Failed to connect to SMTP server. Batch will not be sent.");
                return;
            }

            try
            {
                Debug.WriteLine($"[SmtpLogger] Wysyłanie e-maila z batchem. Liczba wpisów: {batch.Count}");
                var mail = new MimeMessage();
                mail.From.Add(new MailboxAddress(string.Empty, _options.From));
                mail.To.Add(new MailboxAddress(string.Empty, _options.To));
                mail.Subject = $"[Batch] {_options.ServiceName}";

                // Grupowanie identycznych wpisów
                var grouped = batch
                    .GroupBy(e => e.Message)
                    .Select(g => g.Count() == 1
                        ? g.Key
                        : $"<div style=\"color:#888;\">(x{g.Count()})</div><div>{g.Key}</div><hr/>");

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = string.Join("", grouped)
                };

                mail.Body = bodyBuilder.ToMessageBody();

                _client?.Send(mail);
                _isConnected = true;
                CompleteAdding();
            }
            catch (Exception)
            {
                _isConnected = false;
            }
        }

        private void ProcessLogQueue()
        {
            while (true)
            {
                LogMessageEntry message;
                if (!TryDequeue(out message))
                    break;

                lock (_batchLock)
                {
                    _batch.Add(message);
                    Debug.WriteLine($"[SmtpLogger] Dodano wpis do batcha. Liczba wpisów w batchu: {_batch.Count}");
                    if (_batch.Count == 1)
                    {
                        Debug.WriteLine($"[SmtpLogger] Startuję timer batchowania na {_options.BatchTimeoutSeconds} sekund.");
                        _batchTimer?.Dispose();
                        _batchTimer = new Timer(BatchTimeoutCallback, null, _options.BatchTimeoutSeconds * 1000, Timeout.Infinite);
                    }
                    if (_batch.Count >= _options.BatchLimit)
                    {
                        Debug.WriteLine("[SmtpLogger] Osiągnięto BatchLimit, resetuję timer i wysyłam batch.");
                        _batchTimer?.Dispose();
                        SendAndClearBatch();
                    }
                }
            }
        }

        private void BatchTimeoutCallback(object state)
        {
            lock (_batchLock)
            {
                Debug.WriteLine("[SmtpLogger] Upłynął czas batchowania, wysyłam batch i resetuję timer.");
                SendAndClearBatch();
            }
        }

        private void SendAndClearBatch()
        {
            if (_batch.Count > 0)
            {
                var toSend = new List<LogMessageEntry>(_batch);
                _batch.Clear();
                WriteBatch(toSend);
            }
            _batchTimer?.Dispose();
            _batchTimer = null;
        }

        private void ConnectWithRetry()
        {
            for (var attempt = 1; attempt <= MaxConnectAttempts; attempt++)
            {
                try
                {
                    _client = new SmtpClient();

                    _client.Connect(_options.Host, _options.Port, _options.EnableSsl);

                    if (_options.Username != string.Empty || _options.Password != string.Empty)
                        _client.Authenticate(_options.Username, _options.Password);


                    Debug.WriteLine($"Connected to SMTP server successfully on {attempt} attempt.");
                    _isConnected = true;

                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error connecting to SMTP server: {ex.Message}. Attempt: {attempt}");
                    _isConnected = false;
                }

                Thread.Sleep(BaseDelayBetweenConnectAttempts * attempt);
            }
        }

        public bool TryDequeue(out LogMessageEntry item)
        {
            lock (_messageQueue)
            {
                while (_messageQueue.Count == 0)
                {
                    Monitor.Wait(_messageQueue);
                }

                if (_messageQueue.Count > 0)
                {
                    item = _messageQueue.Dequeue();
                    Debug.WriteLine($"[SmtpLogger] Pobrano wiadomość z kolejki. Liczba wpisów w kolejce (po pobraniu): {_messageQueue.Count}");
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
                _client?.Disconnect(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disconnecting from SMTP server: {ex.Message}");
            }
            finally
            {
                _client?.Dispose();
            }

            try
            {
                _outputThread.Join(1500);
            }
            catch (ThreadStateException) { }

            lock (_batchLock)
            {
                _batchTimer?.Dispose();
                _batch.Clear();
            }
        }

        private void CompleteAdding()
        {
            lock (_messageQueue)
            {
                Monitor.PulseAll(_messageQueue);
            }
        }
    }
}
