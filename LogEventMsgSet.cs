using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp
{
    internal class LogEventMsgSet
    {
        private string asyncLogEvent;
        private int currentMessage;

        public LogEventMsgSet(string asyncLogEvent)
        {
            this.asyncLogEvent = asyncLogEvent;
            currentMessage = 0;
        }

        public Task SendAsync(CancellationToken token)
        {
            return SendAsync(token, new TaskCompletionSource<object>());
        }

        private Task SendAsync(CancellationToken token, TaskCompletionSource<object> tcs)
        {
            if (token.IsCancellationRequested)
                return tcs.CanceledTask();

            var allSent = currentMessage > 0;
            if (allSent)
                return tcs.SucceededTask();

            try
            {
                currentMessage++;

                new StringWriter()
                    .WriteAsync(asyncLogEvent)
                    .ContinueWith(t =>
                    {
                        if (t.IsCanceled)
                        {
                            tcs.SetCanceled();
                            return;
                        }
                        if (t.Exception != null)
                        {
                            Console.Error.WriteLine(t.Exception.GetBaseException());
                            tcs.SetException(t.Exception);
                            return;
                        }
                        SendAsync(token, tcs);
                    }, token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);

                return tcs.Task;
            }
            catch (Exception exception)
            {
                return tcs.FailedTask(exception);
            }
        }
    }
}