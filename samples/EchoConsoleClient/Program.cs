using System;
using System.Threading.Tasks;
using WebSocketManager.Client;
using WebSocketManager.Common;

public class Program
{
    private static Connection _connection;
    private static StringMethodInvocationStrategy _strategy;

    public static void Main(string[] args)
    {
        StartConnectionAsync();

        _strategy.On("receiveMessage", (arguments) =>
        {
            Console.WriteLine($"{arguments[0]} said: {arguments[1]}");
        });

        Console.ReadLine();
        StopConnectionAsync();
    }

    public static async Task StartConnectionAsync()
    {
        _strategy = new StringMethodInvocationStrategy();
        _connection = new Connection(_strategy);
        await _connection.StartConnectionAsync("ws://localhost:65110/chat");
    }

    public static async Task StopConnectionAsync()
    {
        await _connection.StopConnectionAsync();
    }
}