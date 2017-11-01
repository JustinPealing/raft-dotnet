using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using ProtoBuf;
using raft_dotnet.Communication;

namespace raft_dotnet.Tcp
{
    public class TcpRaftCommunication : IRaftCommunication, IDisposable
    {
        private string _listenAddress;
        private TcpListener _listener;

        public TcpRaftCommunication(string listenAddress)
        {
            _listenAddress = listenAddress;
        }

        public event EventHandler<RaftMessageEventArgs> Message;

        public void SendAppendEntries(string destination, AppendEntriesArguments message)
        {
            throw new NotImplementedException();
        }

        public void SendAppendEntriesResult(string destination, AppendEntriesResult message)
        {
            throw new NotImplementedException();
        }

        public void SendRequestVote(string destination, RequestVoteArguments message)
        {
            throw new NotImplementedException();
        }

        public void SendRequestVoteResult(string destination, RequestVoteResult message)
        {
            throw new NotImplementedException();
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
                    OnMessage(new RaftMessageEventArgs
                    {
                        Message = (RaftMessage) request.Arguments
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
