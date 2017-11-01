using System;
using System.Threading.Tasks;

namespace raft_dotnet.Communication
{
    public interface IRaftCommunication
    {
        event EventHandler<RaftMessageEventArgs> Message;
        Task<AppendEntriesResult> AppendEntriesAsync(string destination, AppendEntriesArguments message);
        Task<RequestVoteResult> RequestVoteAsync(string destination, RequestVoteArguments message);
    }
}