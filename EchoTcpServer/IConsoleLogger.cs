namespace EchoTcpServer.Services
{
    public interface IConsoleLogger
    {
        void LogInfo(string message);
        void LogError(string message);
    }
    
    public class ConsoleLogger : IConsoleLogger
    {
        public void LogInfo(string message) => Console.WriteLine(message);
        public void LogError(string message) => Console.WriteLine($"Error: {message}");
    }
}
