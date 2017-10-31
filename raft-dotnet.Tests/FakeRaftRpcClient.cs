using System;
using System.Threading.Tasks;

namespace raft_dotnet.Tests
{
    public class FakeRaftRpcClient : IRaftRpcClient
    {
        public RaftNode Remote { get; set; }

        public event EventHandler<RaftMessageEventArgs> Message;

        public Task<RequestVoteResult> RequestVoteAsync(RequestVoteArguments arguments)
        {
            throw new NotImplementedException();
        }

        public Task<AppendEntriesResult> AppendEntriesAsync(AppendEntriesArguments arguments)
        {
            throw new NotImplementedException();
        }
    }
}
