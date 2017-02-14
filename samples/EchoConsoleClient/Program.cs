using System;
using System.Threading.Tasks;
using WebSocketManager.Client;
public class Program 
{
    private static Connection _connection;
    public static void Main(string[] args)
    {
        StartConnectionAsync();
        Console.ReadLine();
        StopConnectionAsync();
    }

    public static async Task StartConnectionAsync()
    {
        _connection = new Connection();
        await _connection.StartConnectionAsync("ws://localhost:5000/test");
    }

    public static async Task StopConnectionAsync()
    {
        await _connection.StopConnectionAsync();
    }
}