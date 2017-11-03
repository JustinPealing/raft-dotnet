using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();
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
        
        private async Task<RaftMessage> SendMessageAsync(string destination, RaftMessage message)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }
            
            var semaphor = GetLock(destination);
            await semaphor.WaitAsync();
            try
            {
                var stream = await GetTcpStream(destination);

                var requestData = Serialize(new MessageWrapper {Message = message});
                var requestHeader = BitConverter.GetBytes(requestData.Length);

                Log.Verbose("Sending {Length} bytes to {Destination} - {RequestData}", requestData.Length, destination, Encoding.UTF8.GetString(requestData));
                await stream.WriteAsync(requestHeader, 0, 4);
                await stream.WriteAsync(requestData, 0, requestData.Length);

                var responseHeader = await stream.ReadExactlyAsync(4);
                var size = BitConverter.ToInt32(responseHeader, 0);
                var responseBody = await stream.ReadExactlyAsync(size);
                Log.Verbose("Recieved {Length} bytes from {Destination} - {RequestData}", size, destination, Encoding.UTF8.GetString(responseBody));

                return Deserialize(responseBody).Message;
            }
            finally
            {
                semaphor.Release();
            }
        }

        private MessageWrapper Deserialize(byte[] responseBody)
        {
            using (var memoryStream = new MemoryStream(responseBody))
            {
                return Serializer.Deserialize<MessageWrapper>(memoryStream);
            }
        }

        private static byte[] Serialize(MessageWrapper message)
        {
            using (var memoryStream = new MemoryStream())
            {
                Serializer.Serialize(memoryStream, message);
                return memoryStream.ToArray();
            }
        }

        private async Task<NetworkStream> GetTcpStream(string destination)
        {
            var client = _clients.GetOrAdd(destination, s => new TcpClient());
            if (!client.Connected)
            {
                var port = int.Parse(destination.Split(":")[1]);
                await client.ConnectAsync("localhost", port);
            }
            var stream = client.GetStream();
            return stream;
        }

        private SemaphoreSlim GetLock(string destination)
        {
            return _locks.GetOrAdd(destination, s => new SemaphoreSlim(1));
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

        private async Task ClientLoop(TcpClient client)
        {
            try
            {
                Log.Information("Client connected");
                await ClientLoopInner(client);
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
            using (var stream = client.GetStream())
            {
                while (client.Connected)
                {
                    var responseHeader = await stream.ReadExactlyAsync(4);
                    var size = BitConverter.ToInt32(responseHeader, 0);

                    var requestData = await stream.ReadExactlyAsync(size);
                    
                    Log.Verbose("Recieved {Length} bytes - {RequestData}", size, Encoding.UTF8.GetString(requestData));

                    var request = Deserialize(requestData);

                    if (request.Message is RequestVoteArguments requestVote)
                    {
                        var result = await Server.RequestVoteAsync(requestVote);

                        var responseData = Serialize(new MessageWrapper { Message = result });
                        var responseSize = BitConverter.GetBytes(responseData.Length);
                        await stream.WriteAsync(responseSize, 0, 4);
                        await stream.WriteAsync(responseData, 0, responseData.Length);
                        
                        Log.Verbose("Sent {Length} bytes - {ResponseData}", responseData.Length, Encoding.UTF8.GetString(responseData));
                    }
                    else if (request.Message is AppendEntriesArguments appendEntries)
                    {
                        var result = await Server.AppendEntriesAsync(appendEntries);

                        var responseData = Serialize(new MessageWrapper { Message = result });
                        var responseSize = BitConverter.GetBytes(responseData.Length);
                        await stream.WriteAsync(responseSize, 0, 4);
                        await stream.WriteAsync(responseData, 0, responseData.Length);

                        Log.Verbose("Sent {Length} bytes - {ResponseData}", responseData.Length, Encoding.UTF8.GetString(responseData));
                    }
                    else
                    {
                        throw new InvalidOperationException("Unknown message");
                    }
                }
            }
        }

        public void Dispose()
        {
            _listener.Server.Dispose();
        }
    }
}
