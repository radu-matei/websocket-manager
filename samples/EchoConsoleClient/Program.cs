using System;
using System.Threading.Tasks;
using WebSocketManager.Client;
public class Program 
{
    public static void Main(string[] args)
    {
        StartConnectionAsync();
        Console.ReadLine();
    }

    public static async Task StartConnectionAsync()
    {
        var connection = new Connection();
        await connection.StartConnectionAsync("ws://localhost:5000/test");
    }
}