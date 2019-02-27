using System;
using System.Globalization;
using System.Text;

namespace ConsoleApp
{
    internal class MessageBuilder
    {
        private const string TimestampFormat = "{0:yyyy-MM-ddTHH:mm:ss.ffffffK}";
        private static readonly byte[] SpaceBytes = { 0x20 };

        public string[] BuildLogEntries(string logEvent, string layout)
        {
            return new[] { logEvent };
        }

        public void PrepareMessage(ByteArray buffer, string logEntry)
        {
            var ascii = new ASCIIEncoding();
            var utf8 = new UTF8Encoding();
            buffer.Reset();
            var pri = $"<{220.ToString(CultureInfo.InvariantCulture)}>";
            var timestamp = string.Format(CultureInfo.InvariantCulture, TimestampFormat, DateTime.Now);
            var header = $"{123}{2.0} {timestamp} mymachine appName {123} {89}";
            buffer.Append(ascii.GetBytes(header));
            buffer.Append(SpaceBytes);
            buffer.Append(utf8.GetPreamble());
            buffer.Append(utf8.GetBytes(logEntry));
        }
    }
}