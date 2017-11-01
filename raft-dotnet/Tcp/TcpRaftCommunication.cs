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
        private readonly string _listenAddress;
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
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            Task.Run(async () =>
            {
                Log.Verbose("Sending {@Message} to {Destination}", message, destination);
                var client = _clients.GetOrAdd(destination, s => new TcpClient());
                if (!client.Connected)
                {
                    var port = int.Parse(destination.Split(":")[1]);
                    await client.ConnectAsync("localhost", port);
                }
                
                var data = Serialize(message);
                await client.GetStream().WriteAsync(data, 0, data.Length);
            }).ContinueWith(task =>
            {
                if (task.Exception != null)
                {
                    Log.Verbose(task.Exception, "Error sending message: {Message}");
                }
            });
        }
        
        private static byte[] Serialize(RaftMessage arguments)
        {
            using (var memoryStream = new MemoryStream())
            {
                Serializer.SerializeWithLengthPrefix(memoryStream, new MessageWrapper {Message = arguments}, PrefixStyle.Base128);
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
                Task.Run(() => ClientLoop(client));
                BeginAcceptClient(listener);
            }, null);
        }

        private void ClientLoop(TcpClient client)
        {
            try
            {
                Log.Information("Client connected");
                ClientLoopInner(client);
            }
            catch (IOException ex)
            {
                if (ex.InnerException is SocketException socketException)
                {
                    Log.Warning("SocketException: {Errorcode}", socketException.SocketErrorCode);
                }
                else
                {
                    Log.Error(ex, "Unhandled error in TCP server loop");
                }
            }
            catch (SocketException ex)
            {
                Log.Warning("SocketException: {Errorcode}", ex.SocketErrorCode);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unhandled error in TCP server loop");
            }
            Log.Information("Client disconnected");
        }

        private void ClientLoopInner(TcpClient client)
        {
            using (var stream = client.GetStream())
            {
                while (client.Connected)
                {
                    var request = Serializer.DeserializeWithLengthPrefix<MessageWrapper>(stream, PrefixStyle.Base128);
                    Log.Verbose("Message recieved {@Message}", request.Message);
                    OnMessage(new RaftMessageEventArgs {Message = request.Message});
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
