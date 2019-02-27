using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp
{
    internal class MessageTransmitter
    {
        protected static readonly TimeSpan ZeroSeconds = TimeSpan.FromSeconds(0);

        private volatile bool neverCalledInit;
        private volatile bool isReady;
        private readonly TimeSpan newInitDelay;
        private UdpClient udp;

        protected string Server { get; }

        protected string IpAddress => Dns.GetHostAddresses(Server).FirstOrDefault()?.ToString();

        protected int Port { get; }

        public MessageTransmitter(string server, int port, int reconnectInterval)
        {
            neverCalledInit = true;
            isReady = false;
            newInitDelay = TimeSpan.FromMilliseconds(reconnectInterval);
            Server = server;
            Port = port;
        }

        public Task SendMessageAsync(ByteArray message, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return Task.FromResult<object>(null);

            return PrepareForSendAsync(token)
                .Then(_ => SendAsync(message, token), token)
                .Unwrap()
                .ContinueWith(t =>
                {
                    if (t.Exception == null) // t.IsFaulted is false
                        return Task.FromResult<object>(null);

                    Console.Error.WriteLine(t.Exception?.GetBaseException().StackTrace, "SendAsync failed");
                    TidyUp();
                    return SendMessageAsync(message, token); // Failures impact on the log entry queue
                }, token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current)
                .Unwrap();
        }

        public void Dispose()
        {
            TidyUp();
        }

        private Task PrepareForSendAsync(CancellationToken token)
        {
            if (isReady)
                return Task.FromResult<object>(null);

            var delay = neverCalledInit ? ZeroSeconds : newInitDelay;
            neverCalledInit = false;
            return Task
                .Delay(delay, token)
                .Then(_ => Init(), token)
                .Unwrap()
                .Then(_ => isReady = true, token);
        }

        private void TidyUp()
        {
            try
            {
                if (isReady)
                    Terminate();
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.StackTrace, "Terminate failed");
            }
            finally
            {
                isReady = false;
            }
        }

        private Task Init()
        {
            udp = new UdpClient(IpAddress, Port);
            return Task.FromResult<object>(null);
        }

        private Task SendAsync(ByteArray message, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return Task.FromResult<object>(null);
            return udp.SendAsync(message, message.Length);
        }

        private void Terminate()
        {
            udp?.Close();
        }
    }
}