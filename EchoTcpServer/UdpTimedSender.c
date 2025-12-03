using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using EchoTcpServer.Networking;

namespace EchoTcpServer.Services
{
    public class UdpTimedSender : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly IUdpClient _udpClient;
        private readonly IConsoleLogger _logger;
        private Timer _timer;
        private ushort _counter = 0;
        private readonly Random _random = new Random();

        public UdpTimedSender(string host, int port, IUdpClient udpClient = null, IConsoleLogger logger = null)
        {
            _host = host;
            _port = port;
            _udpClient = udpClient ?? new UdpClientWrapper();
            _logger = logger ?? new ConsoleLogger();
        }

        public void StartSending(int intervalMilliseconds)
        {
            if (_timer != null)
                throw new InvalidOperationException("Sender is already running.");

            _timer = new Timer(SendMessageCallback, null, 0, intervalMilliseconds);
            _logger.LogInfo($"Started sending UDP messages every {intervalMilliseconds}ms to {_host}:{_port}");
        }

        private void SendMessageCallback(object state)
        {
            try
            {
                byte[] samples = new byte[1024];
                _random.NextBytes(samples);
                _counter++;

                byte[] header = new byte[] { 0x04, 0x84 };
                byte[] counterBytes = BitConverter.GetBytes(_counter);
                byte[] message = header.Concat(counterBytes).Concat(samples).ToArray();

                var endpoint = new IPEndPoint(IPAddress.Parse(_host), _port);
                _udpClient.Send(message, message.Length, endpoint);
                
                _logger.LogInfo($"Message #{_counter} sent to {_host}:{_port}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending message: {ex.Message}");
            }
        }

        public void StopSending()
        {
            _timer?.Dispose();
            _timer = null;
            _logger.LogInfo("Stopped sending UDP messages");
        }

        public void Dispose()
        {
            StopSending();
            _udpClient.Dispose();
        }
    }
}
