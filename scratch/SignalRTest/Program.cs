
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

class Program {
    static async Task Main(string[] args) {
        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5172/hubs/privatechat")
            .Build();
        
        await connection.StartAsync();
        Console.WriteLine("Connected");
        try {
            await connection.InvokeAsync("SendPrivateMessage", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "Hello", null);
            Console.WriteLine("Invoked successfully");
        } catch (Exception ex) {
            Console.WriteLine("Exception: " + ex.Message);
        }
    }
}

