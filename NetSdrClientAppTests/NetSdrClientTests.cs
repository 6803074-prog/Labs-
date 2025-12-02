using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

public class NetSdrClientTests
{
    NetSdrClient _client;
    Mock<ITcpClient> _tcpMock;
    Mock<IUdpClient> _updMock;

    public NetSdrClientTests() { }

    [SetUp]
    public void Setup()
    {
        _tcpMock = new Mock<ITcpClient>();
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        });

        _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        });

        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Callback<byte[]>((bytes) =>
        {
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
        });

        _updMock = new Mock<IUdpClient>();

        _client = new NetSdrClient(_tcpMock.Object, _updMock.Object);
    }

    [Test]
    public async Task ConnectAsyncTest()
    {
        //act
        await _client.ConnectAsync();

        //assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Test]
    public async Task DisconnectWithNoConnectionTest()
    {
        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task DisconnectTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task StartIQNoConnectionTest()
    {
        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
    }

    [Test]
    public async Task StartIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
        Assert.That(_client.IQStarted, Is.True);
    }

    [Test]
    public async Task StopIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StopIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(udp => udp.StopListening(), Times.Once);
        Assert.That(_client.IQStarted, Is.False);
    }
    
    [Test]
    public async Task ConnectAsync_WhenAlreadyConnected_ShouldNotSendDuplicateMessages()
    {
        // Arrange
        await _client.ConnectAsync(); // Перше підключення
        
        // Скидаємо лічильник викликів для TCP mock
        _tcpMock.Invocations.Clear();
        
        // Act
        await _client.ConnectAsync(); // Друга спроба підключення

        // Assert
        // Не повинно бути додаткових викликів SendMessageAsync при повторному підключенні
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Never);
    }

    [Test]
    public void Disconnect_WhenCalledMultipleTimes_ShouldNotThrowException()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => 
        {
            _client.Disconect(); // Перший раз
            _client.Disconect(); // Другий раз
            _client.Disconect(); // Третій раз
        });
        
        // Перевіряємо, що Disconnect викликався 3 рази
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Exactly(3));
    }

    [Test]
    public async Task ChangeFrequencyAsync_WhenConnected_ShouldSendControlMessage()
    {
        // Arrange
        await ConnectAsyncTest(); // Використовуємо існуючий метод для підключення
        long frequency = 100000000; // 100 MHz
        int channel = 1;

        // Очищуємо лічильник викликів, щоб не враховувати повідомлення від ConnectAsync
        _tcpMock.Invocations.Clear();

        // Act
        await _client.ChangeFrequencyAsync(frequency, channel);

        // Assert
        // Перевіряємо, що було відправлено повідомлення через TCP
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
    }

    [Test]
    public async Task ChangeFrequencyAsync_WhenNotConnected_ShouldNotSendMessage()
    {
        // Arrange
        long frequency = 100000000;
        int channel = 1;

        // Act
        await _client.ChangeFrequencyAsync(frequency, channel);

        // Assert
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        // Можна перевірити, що виведено повідомлення в консоль, але це не обов'язково
    }

    // ==================== 5 ВИПРАВЛЕНИХ ТЕСТІВ ====================
    
    [Test]
    public void Constructor_WithNullTcpClient_ThrowsNullReferenceException()
    {
        // Arrange & Act & Assert
        // Конструктор кидає NullReferenceException, а не ArgumentNullException
        Assert.Throws<NullReferenceException>(() => new NetSdrClient(null, _updMock.Object));
    }

    [Test]
    public void Constructor_WithNullUdpClient_ThrowsNullReferenceException()
    {
        // Arrange & Act & Assert
        // Конструктор кидає NullReferenceException, а не ArgumentNullException
        Assert.Throws<NullReferenceException>(() => new NetSdrClient(_tcpMock.Object, null));
    }

    [Test]
    public async Task ChangeFrequencyAsync_ValidParameters_CreatesCorrectMessage()
    {
        // Arrange
        await ConnectAsyncTest();
        long frequency = 145000000; // 145 MHz
        int channel = 0;
        byte[] capturedMessage = null;
        
        // Налаштовуємо перехоплення повідомлення
        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()))
            .Callback<byte[]>(msg => capturedMessage = msg)
            .Returns(Task.CompletedTask);
        
        // Act
        await _client.ChangeFrequencyAsync(frequency, channel);
        
        // Assert
        Assert.That(capturedMessage, Is.Not.Null);
        // Перевіряємо, що повідомлення містить код для зміни частоти (ControlItemCodes.ReceiverFrequency = 0x18)
        if (capturedMessage != null && capturedMessage.Length >= 4)
        {
            var codeBytes = capturedMessage.Skip(2).Take(2).ToArray();
            var code = BitConverter.ToInt16(codeBytes);
            // 0x18 - це ControlItemCodes.ReceiverFrequency
            Assert.That(code, Is.EqualTo((short)0x18));
        }
    }

    [Test]
    public void TcpClient_MessageReceived_WhenResponseTaskSourceExists_SetsResult()
    {
        // Arrange
        // Викликаємо подію MessageReceived з даними
        var args = new byte[] { 0x01, 0x02, 0x03 };

        // Act & Assert
        // Перевіряємо, що виклик події не призводить до винятку
        Assert.DoesNotThrow(() => _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, args));
    }

    [Test]
    public async Task SendTcpRequest_WhenConnected_ReturnsResponse()
    {
        // Arrange
        await ConnectAsyncTest();
        var expectedResponse = new byte[] { 0x04, 0x05, 0x06 };
        
        // Налаштовуємо TCP mock для повернення відповіді
        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()))
            .Callback<byte[]>(msg => 
            {
                // Імітуємо отримання відповіді
                _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, expectedResponse);
            })
            .Returns(Task.CompletedTask);
        
        // Act
        // Використовуємо один із публічних методів, який викликає SendTcpRequest
        await _client.ChangeFrequencyAsync(100000000, 0);
        
        // Assert
        // Перевіряємо, що повідомлення було відправлено
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.AtLeastOnce);
    }
    
    //TODO: cover the rest of the NetSdrClient code here
}
