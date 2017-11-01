using System;
using raft_dotnet;
using raft_dotnet.Tcp;

namespace ConsoleApp3
{
    class Program
    {
        static void Main(string[] args)
        {
            var nodes = new[]
            {
                "localhost:13000",
                "localhost:13001",
                "localhost:13002"
            };

            var index = int.Parse(args[0]);
            using (var communication = new TcpRaftCommunication(nodes[index]))
            {
                communication.Start();

                var node = new RaftNode(communication, nodes);
                node.Start();
                Console.ReadLine();
            }
        }
    }
}
