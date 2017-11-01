using System;
using System.Collections.Generic;
using System.Linq;
using raft_dotnet;
using raft_dotnet.Tcp;

namespace ConsoleApp3
{
    class Program
    {
        static void Main(string[] args)
        {
            var ports = new List<int>
            {
                13000,
                13001,
                13002
            };
            var index = int.Parse(args[0]);
            var port = ports[index];
            ports.RemoveAt(index);

            var node = new RaftNode(ports.Select(p => new TcpRaftClient(port)).ToArray());
            node.Start();

            var server = new TcpRaftServer(node);
            server.Start();

            Console.ReadLine();
        }
    }
}
