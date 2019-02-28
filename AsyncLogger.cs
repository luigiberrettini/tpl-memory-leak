using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp
{

    internal class AsyncLogger
    {
        private readonly CancellationTokenSource cts;
        private readonly CancellationToken token;
        private readonly BlockingCollection<string> queue;

        public AsyncLogger()
        {
            cts = new CancellationTokenSource();
            token = cts.Token;
            queue = new BlockingCollection<string>();
            Task.Run(() => ProcessQueueAsync());
        }

        public void Log(string asyncLogEvent)
        {
            queue.TryAdd(asyncLogEvent, Timeout.Infinite, token);
        }

        private void ProcessQueueAsync()
        {
            ProcessQueueAsync(new TaskCompletionSource<object>())
                .ContinueWith(t =>
                {
                    Console.Error.WriteLine(t.Exception?.GetBaseException());
                    ProcessQueueAsync();
                }, token, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Current);
        }

        private Task ProcessQueueAsync(TaskCompletionSource<object> tcs)
        {
            if (token.IsCancellationRequested)
                return tcs.CanceledTask();

            try
            {
                var asyncLogEventInfo = queue.Take(token);
                var logEventMsgSet = new LogEventMsgSet(asyncLogEventInfo);

                logEventMsgSet
                    .SendAsync(token)
                    .ContinueWith(t =>
                    {
                        if (t.IsCanceled)
                        {
                            Console.Error.WriteLine("Task canceled");
                            tcs.SetCanceled();
                            return;
                        }
                        if (t.Exception != null) // t.IsFaulted is true
                            Console.Error.WriteLine(t.Exception.GetBaseException().StackTrace, "Task faulted");
                        else
                            Console.WriteLine($"Successfully logged '{asyncLogEventInfo}'");
                        ProcessQueueAsync(tcs);
                    }, token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);

                return tcs.Task;
            }
            catch (Exception exception)
            {
                return tcs.FailedTask(exception);
            }
        }

        public void Dispose()
        {
            cts.Cancel();
            cts.Dispose();
            queue.Dispose();
        }
    }
}