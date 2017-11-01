using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using ProtoBuf;
using raft_dotnet.Communication;

namespace raft_dotnet.Tcp
{
    public class TcpRaftServer
    {
        private readonly RaftNode _node;
        private TcpListener _listener;

        public TcpRaftServer(RaftNode node)
        {
            _node = node;
        }

        public void Start()
        {
            _listener = new TcpListener(IPAddress.Loopback, 13000);
            _listener.Start();
            BeginAcceptClient(_listener);
        }

        private void BeginAcceptClient(TcpListener listener)
        {
            listener.BeginAcceptTcpClient(ar =>
            {
                var client = listener.EndAcceptTcpClient(ar);
                ClientLoop(client);
                BeginAcceptClient(listener);
            }, null);
        }

        private async Task ClientLoop(TcpClient client)
        {
            try
            {
                await ClientLoopInner(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            Console.WriteLine("Client disconnected");
        }

        private async Task ClientLoopInner(TcpClient client)
        {
            Console.WriteLine("Client connected");
            using (var stream = client.GetStream())
            {
                while (client.Connected)
                {
                    var request = Serializer.Deserialize<Request>(stream);
                    if (request.Arguments is RequestVoteArguments)
                    {
                        var result = _node.RequestVote((RequestVoteArguments)request.Arguments);
                        Serializer.Serialize(stream, new Response
                        {
                            Result = result,
                            CorrelationId = request.CorrelationId
                        });
                    }
                    if (request.Arguments is AppendEntriesArguments)
                    {
                        var result = _node.AppendEntries((AppendEntriesArguments)request.Arguments);
                        Serializer.Serialize(stream, new Response
                        {
                            Result = result,
                            CorrelationId = request.CorrelationId
                        });
                    }
                }
            }
        }
    }
}