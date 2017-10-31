using System.Threading.Tasks;

namespace raft_dotnet.Tests
{
    public class FakeRaftRpcClient : IRaftRpcClient
    {
        public RaftNode Remote { get; set; }
        
        public async Task<RequestVoteResult> RequestVoteAsync(RequestVoteArguments arguments)
        {
            await Task.Delay(50);
            return await Remote.RequestVoteAsync(arguments);
        }

        public async Task<AppendEntriesResult> AppendEntriesAsync(AppendEntriesArguments arguments)
        {
            await Task.Delay(50);
            return await Remote.AppendEntriesAsync(arguments);
        }
    }
}
