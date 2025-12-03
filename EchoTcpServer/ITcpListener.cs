using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EchoTcpServer.Services
{
    public interface ITcpListener
    {
        void Start();
        void Stop();
        Task<TcpClient> AcceptTcpClientAsync();
        bool Pending();
    }
}
