using System;
using raft_dotnet;
using raft_dotnet.Tcp;
using Serilog;

namespace ConsoleApp3
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.ColoredConsole()
                .CreateLogger();

            var nodes = new[]
            {
                "localhost:13000",
                "localhost:13001",
                "localhost:13002"
            };

            var index = int.Parse(args[0]);
            Console.WriteLine($"Listening on {nodes[index]}");
            using (var communication = new TcpRaftCommunication(nodes[index]))
            {
                communication.Start();

                var node = new RaftNode(communication, nodes, nodes[index])
                {
                    MinEllectionTimeout = TimeSpan.FromMilliseconds(1500),
                    MaxEllectionTimeout = TimeSpan.FromMilliseconds(3000)
                };
                node.Start();
                Console.ReadLine();
            }
        }
    }
}
