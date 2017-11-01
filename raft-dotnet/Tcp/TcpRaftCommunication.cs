using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using ProtoBuf;
using raft_dotnet.Communication;
using Serilog;

namespace raft_dotnet.Tcp
{
    public class TcpRaftCommunication : IRaftCommunication, IDisposable
    {
        private string _listenAddress;
        private readonly ConcurrentDictionary<string, TcpClient> _clients = new ConcurrentDictionary<string, TcpClient>();
        private TcpListener _listener;

        public TcpRaftCommunication(string listenAddress)
        {
            _listenAddress = listenAddress;
        }

        public event EventHandler<RaftMessageEventArgs> Message;

        public void SendAppendEntries(string destination, AppendEntriesArguments message)
        {
            SendMessage(destination, message);
        }

        public void SendAppendEntriesResult(string destination, AppendEntriesResult message)
        {
            SendMessage(destination, message);
        }

        public void SendRequestVote(string destination, RequestVoteArguments message)
        {
            SendMessage(destination, message);
        }

        public void SendRequestVoteResult(string destination, RequestVoteResult message)
        {
            SendMessage(destination, message);
        }

        private void SendMessage(string destination, RaftMessage message)
        {
            Task.Run(async () =>
            {
                var client = _clients.GetOrAdd(destination, s => new TcpClient());
                if (!client.Connected)
                {
                    var port = int.Parse(destination.Split(":")[1]);
                    await client.ConnectAsync("localhost", port);
                }
                
                var data = Serialize(message);
                await client.GetStream().WriteAsync(data, 0, data.Length);
            });
        }
        
        private static byte[] Serialize(RaftMessage arguments)
        {
            using (var memoryStream = new MemoryStream())
            {
                Serializer.Serialize(memoryStream, new MessageWrapper {Message = arguments});
                return memoryStream.ToArray();
            }
        }

        public void Start()
        {
            var port = int.Parse(_listenAddress.Split(":")[1]);
            _listener = new TcpListener(IPAddress.Loopback, port);
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
                Log.Error(ex, "Error in TCP client loop");
            }
            Log.Information("Client disconnected");
        }

        private async Task ClientLoopInner(TcpClient client)
        {
            Log.Information("Client connected");
            using (var stream = client.GetStream())
            {
                while (client.Connected)
                {
                    var request = Serializer.Deserialize<MessageWrapper>(stream);
                    OnMessage(new RaftMessageEventArgs
                    {
                        Message = request.Message
                    });
                }
            }
        }

        public void Dispose()
        {
            _listener.Server.Dispose();
        }

        protected virtual void OnMessage(RaftMessageEventArgs e)
        {
            Message?.Invoke(this, e);
        }
    }
}
