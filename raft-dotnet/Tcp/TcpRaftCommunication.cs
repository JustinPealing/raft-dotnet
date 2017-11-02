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

        public IRaftRpc Server { get; set; }

        public async Task<AppendEntriesResult> AppendEntriesAsync(string destination, AppendEntriesArguments message)
        {
            return (AppendEntriesResult)await SendMessageAsync(destination, message);
        }
        
        public async Task<RequestVoteResult> RequestVoteAsync(string destination, RequestVoteArguments message)
        {
            return (RequestVoteResult) await SendMessageAsync(destination, message);
        }
        
        private async Task<object> SendMessageAsync(string destination, RaftMessage message)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }
            
            Log.Verbose("Sending {@Message} to {Destination}", message, destination);
            var client = _clients.GetOrAdd(destination, s => new TcpClient());
            if (!client.Connected)
            {
                var port = int.Parse(destination.Split(":")[1]);
                await client.ConnectAsync("localhost", port);
            }

            var data = Serialize(message);
            var stream = client.GetStream();
            await stream.WriteAsync(data, 0, data.Length);

            var response = Serializer.DeserializeWithLengthPrefix<MessageWrapper>(stream, PrefixStyle.Base128);
            Log.Verbose("Recieved response from {Destination}", destination);
            return response;
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

        private async Task ClientLoopInner(TcpClient client)
        {
            try
            {
                using (var stream = client.GetStream())
                {
                    while (client.Connected)
                    {
                        var request = Serializer.DeserializeWithLengthPrefix<MessageWrapper>(stream, PrefixStyle.Base128);
                        Log.Verbose("Message recieved {@Message}", request.Message);

                        if (request.Message is RequestVoteArguments requestVote)
                        {
                            var result = await Server.RequestVoteAsync(requestVote);
                            var data = Serialize(result);
                            Log.Verbose("Responding to RequestVoteArguments");
                            await stream.WriteAsync(data, 0, data.Length);
                        }
                        else if (request.Message is AppendEntriesArguments appendEntries)
                        {
                            var result = await Server.AppendEntriesAsync(appendEntries);
                            var data = Serialize(result);
                            Log.Verbose("Responding to AppendEntriesArguments");
                            await stream.WriteAsync(data, 0, data.Length);
                        }
                        else
                        {
                            throw new InvalidOperationException("Unknown message");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Client loop error");
            }
        }

        public void Dispose()
        {
            _listener.Server.Dispose();
        }
    }
}
