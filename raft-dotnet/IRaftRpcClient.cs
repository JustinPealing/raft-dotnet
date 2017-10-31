using System;
using System.Threading.Tasks;

namespace raft_dotnet
{
    public interface IRaftRpcClient
    {
        event EventHandler<RaftMessageEventArgs> Message;

        Task<RequestVoteResult> RequestVoteAsync(RequestVoteArguments arguments);

        Task<AppendEntriesResult> AppendEntriesAsync(AppendEntriesArguments arguments);
    }
}
