using System.Threading;
using System.Threading.Tasks;

namespace EchoTcpServer.Services
{
    public interface IEchoService
    {
        Task StartAsync(CancellationToken cancellationToken = default);
        void Stop();
    }
}
