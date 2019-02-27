using System;

namespace ConsoleApp
{
    public struct AsyncLogEventInfo
    {
        public AsyncLogEventInfo(string logEvent)
        {
            LogEvent = logEvent;
        }

        public void Continuation(Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
        }

        public string LogEvent { get; }
    }
}