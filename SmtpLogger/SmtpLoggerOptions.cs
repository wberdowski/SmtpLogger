namespace SmtpLogger
{
    public sealed class SmtpLoggerOptions
    {
        public static int DefaultMaxQueueLengthValue { get; set; } = 100;
        public int? MaxQueueLength { get; set; }
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; }
    }

}
