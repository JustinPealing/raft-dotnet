using System;
using System.Threading.Tasks;

namespace raft_dotnet.Communication
{
    public interface IRaftCommunication
    {
        event EventHandler<RaftMessageEventArgs> Message;
        Task<AppendEntriesResult> AppendEntriesAsync(string destination, AppendEntriesArguments message);
        void SendAppendEntriesResult(string destination, AppendEntriesResult message);
        void SendRequestVote(string destination, RequestVoteArguments message);
        void SendRequestVoteResult(string destination, RequestVoteResult message);
    }
}