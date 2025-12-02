using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NetSdrClientApp.Tests
{
    public class NetSdrClientTests
    {
        private readonly Mock<ITcpClient> tcpMock;
        private readonly Mock<IUdpClient> udpMock;
        private readonly NetSdrClient client;

        public NetSdrClientTests()
        {
            tcpMock = new Mock<ITcpClient>();
            udpMock = new Mock<IUdpClient>();

            tcpMock.Setup(t => t.Connected).Returns(true); // default state true
            udpMock.Setup(u => u.StartListeningAsync()).Returns(Task.CompletedTask);

            client = new NetSdrClient(tcpMock.Object, udpMock.Object);
        }

        // ------------------------------------------
        // 1) ConnectAsync should send 3 init commands
        // ------------------------------------------
        [Fact]
        public async Task ConnectAsync_SendsThreeInitializationMessages()
        {
            tcpMock.Setup(t => t.Connected).Returns(false);
            tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);

            // emulate instant response for SendTcpRequest
            tcpMock.SetupAdd(t => t.MessageReceived += It.IsAny<EventHandler<byte[]>>())
                   .Callback<EventHandler<byte[]>>(h => h.Invoke(this, new byte[] { 0x00 }));

            await client.ConnectAsync();

            tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
        }

        // ------------------------------------------
        // 2) StartIQAsync should send a start command
        // ------------------------------------------
        [Fact]
        public async Task StartIQAsync_SendsStartCommand()
        {
            byte[]? sent = null;

            tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>()))
                   .Callback<byte[]>(msg => sent = msg)
                   .Returns(Task.CompletedTask);

            // unlock awaiting response inside SendTcpRequest
            tcpMock.SetupAdd(t => t.MessageReceived += It.IsAny<EventHandler<byte[]>>())
                   .Callback<EventHandler<byte[]>>(h => h.Invoke(this, new byte[] { 0x00 }));

            await client.StartIQAsync();

            Assert.NotNull(sent);
            Assert.True(client.IQStarted);
        }

        // ------------------------------------------
        // 3) StopIQAsync should send a stop command
        // ------------------------------------------
        [Fact]
        public async Task StopIQAsync_SendsStopCommand()
        {
            byte[]? sent = null;

            tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>()))
                   .Callback<byte[]>(msg => sent = msg)
                   .Returns(Task.CompletedTask);

            // emulate response
            tcpMock.SetupAdd(t => t.MessageReceived += It.IsAny<EventHandler<byte[]>>())
                   .Callback<EventHandler<byte[]>>(h => h.Invoke(this, new byte[] { 0x00 }));

            await client.StopIQAsync();

            Assert.Contains((byte)0x01, sent!); // stop flag present
            Assert.False(client.IQStarted);
        }

        // ------------------------------------------
        // 4) ChangeFrequencyAsync should embed frequency into message
        // ------------------------------------------
        [Fact]
        public async Task ChangeFrequencyAsync_SendsCorrectFrequency()
        {
            long freq = 12345678;
            int channel = 2;

            byte[]? sent = null;

            tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>()))
                   .Callback<byte[]>(msg => sent = msg)
                   .Returns(Task.CompletedTask);

            tcpMock.SetupAdd(t => t.MessageReceived += It.IsAny<EventHandler<byte[]>>())
                   .Callback<EventHandler<byte[]>>(h => h.Invoke(this, new byte[] { 0x00 }));

            await client.ChangeFrequencyAsync(freq, channel);

            Assert.NotNull(sent);
            Assert.Contains((byte)channel, sent!);

            var freqBytes = BitConverter.GetBytes(freq).Take(5);
            foreach (var b in freqBytes)
                Assert.Contains(b, sent!);
        }

        // ------------------------------------------
        // 5) TcpClient MessageReceived should complete awaited request
        // ------------------------------------------
        [Fact]
        public async Task TcpClient_MessageReceived_CompletesResponse()
        {
            tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>()))
                   .Returns(Task.CompletedTask);

            // start request
            var task = client
                .GetType()
                .GetMethod("SendTcpRequest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(client, new object[] { new byte[] { 0x10 } }) as Task<byte[]>;

            // send fake TCP response
            byte[] response = new byte[] { 0xAA, 0xBB };
            tcpMock.Raise(t => t.MessageReceived += null, client, response);

            var result = await task;

            Assert.Equal(response, result);
        }
    }
}
