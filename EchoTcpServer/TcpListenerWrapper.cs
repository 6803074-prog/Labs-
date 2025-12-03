using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace EchoTcpServer.Services
{
    public class TcpListenerWrapper : ITcpListener
    {
        private readonly TcpListener _listener;

        public TcpListenerWrapper(IPAddress address, int port)
        {
            _listener = new TcpListener(address, port);
        }

        public void Start() => _listener.Start();
        public void Stop() => _listener.Stop();
        
        public async Task<TcpClient> AcceptTcpClientAsync()
            => await _listener.AcceptTcpClientAsync();
            
        public bool Pending() => _listener.Pending();
    }
}
