using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;

namespace raft_dotnet.Tcp
{
    public class TcpRaftClient : IRaftRpcClient
    {
        private readonly int _port;
        private readonly TcpClient _client;
        private int _correlationId;
        private NetworkStream _stream;

        private IDictionary<int, TaskCompletionSource<object>> _requests = new ConcurrentDictionary<int, TaskCompletionSource<object>>();

        public TcpRaftClient(int port)
        {
            _port = port;
            _client = new TcpClient();
        }

        public async Task<RequestVoteResult> RequestVoteAsync(RequestVoteArguments arguments)
        {
            if (!_client.Connected)
            {
                await _client.ConnectAsync("localhost", _port);
                _stream = _client.GetStream();
                Task.Run(ReadResponse);
            }
            
            var request = new Request
            {
                CorrelationId = Interlocked.Increment(ref _correlationId),
                Arguments = arguments
            };

            var tcs = new TaskCompletionSource<object>();
            _requests[request.CorrelationId] = tcs;

            var data = Serialize(request);
            await _stream.WriteAsync(data, 0, data.Length);

            return (RequestVoteResult) await tcs.Task;
        }

        public async Task<AppendEntriesResult> AppendEntriesAsync(AppendEntriesArguments arguments)
        {
            if (!_client.Connected)
            {
                await _client.ConnectAsync("localhost", _port);
                _stream = _client.GetStream();
                Task.Run(ReadResponse);
            }

            var request = new Request
            {
                CorrelationId = Interlocked.Increment(ref _correlationId),
                Arguments = arguments
            };

            var tcs = new TaskCompletionSource<object>();
            _requests[request.CorrelationId] = tcs;

            var data = Serialize(request);
            await _stream.WriteAsync(data, 0, data.Length);

            return (AppendEntriesResult)await tcs.Task;
        }

        private static byte[] Serialize(object arguments)
        {
            using (var memoryStream = new MemoryStream())
            {
                Serializer.Serialize(memoryStream, arguments);
                return memoryStream.ToArray();
            }
        }

        private async Task ReadResponse()
        {
            while (true)
            {
                var response = Serializer.Deserialize<Response>(_stream);
                _requests[response.CorrelationId].SetResult(response.Result);
            }
        }
    }
}