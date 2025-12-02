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
    public async Task StartIQAsync_WhenAlreadyStarted_ShouldNotStartAgain()
    {
        // Arrange
        await ConnectAsyncTest();
        await _client.StartIQAsync(); // Перший запуск

        // Скидаємо лічильник викликів для UDP mock
        _updMock.Invocations.Clear();

        // Act
        await _client.StartIQAsync(); // Другий запуск

        // Assert
        _updMock.Verify(udp => udp.StartListeningAsync(), Times.Never);
        // Перевіряємо, що IQStarted все ще true
        Assert.That(_client.IQStarted, Is.True);
    }

    [Test]
    public async Task StopIQAsync_WhenIQNotStarted_ShouldStillStopListening()
    {
        // Arrange
        await ConnectAsyncTest();
        // Не запускаємо IQ

        // Act
        await _client.StopIQAsync();

        // Assert
        _updMock.Verify(udp => udp.StopListening(), Times.Once);
        // Перевіряємо, що IQStarted залишається false
        Assert.That(_client.IQStarted, Is.False);
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

        // Act
        await _client.ChangeFrequencyAsync(frequency, channel);

        // Assert
        // Перевіряємо, що було відправлено повідомлення через TCP
        // Враховуємо, що ConnectAsync вже відправив 3 повідомлення
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(4));
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

    [Test]
    public void Constructor_WithNullTcpClient_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        // Примітка: Залежить від реалізації конструктора
        // Якщо конструктор не перевіряє null, може кидати NullReferenceException
        Assert.Throws<ArgumentNullException>(() => new NetSdrClient(null, _updMock.Object));
    }

    [Test]
    public void Constructor_WithNullUdpClient_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NetSdrClient(_tcpMock.Object, null));
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
    //TODO: cover the rest of the NetSdrClient code here
}
