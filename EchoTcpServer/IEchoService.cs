using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EchoTcpServer.Networking;

namespace EchoTcpServer.Services
{
    public class EchoService : IEchoService
    {
        private readonly ITcpListener _listener;
        private readonly IConsoleLogger _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;
        
        public EchoService(ITcpListener listener, IConsoleLogger logger = null)
        {
            _listener = listener ?? throw new ArgumentNullException(nameof(listener));
            _logger = logger ?? new ConsoleLogger();
            _cancellationTokenSource = new CancellationTokenSource();
        }
        
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _listener.Start();
            _logger.LogInfo($"Server started on port.");
            
            var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(
                _cancellationTokenSource.Token, 
                cancellationToken
            ).Token;

            while (!linkedToken.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    _logger.LogInfo("Client connected.");
                    
                    _ = Task.Run(() => HandleClientAsync(client, linkedToken));
                }
                catch (ObjectDisposedException)
                {
                    // Listener has been closed
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error accepting client: {ex.Message}");
                }
            }
        }
        
        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                try
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;

                    while (!token.IsCancellationRequested && 
                           (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                    {
                        await stream.WriteAsync(buffer, 0, bytesRead, token);
                        _logger.LogInfo($"Echoed {bytesRead} bytes to the client.");
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error handling client: {ex.Message}");
                }
                finally
                {
                    _logger.LogInfo("Client disconnected.");
                }
            }
        }
        
        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener.Stop();
            _logger.LogInfo("Server stopped.");
        }
    }
}
