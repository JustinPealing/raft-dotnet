using System;
using System.Threading.Tasks;

namespace raft_dotnet
{
    /// <summary>
    /// Provides communication with another node in the cluster.
    /// </summary>
    public interface IRaftRpcClient
    {
        event EventHandler<RaftMessageEventArgs> Message;

        Task<RequestVoteResult> RequestVoteAsync(RequestVoteArguments arguments);

        Task<AppendEntriesResult> AppendEntriesAsync(AppendEntriesArguments arguments);
    }
}
