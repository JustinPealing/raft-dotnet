using System;

namespace raft_dotnet.Communication
{
    public interface IRaftCommunication
    {
        event EventHandler<RaftMessageEventArgs> Message;
        void SendAppendEntries(string destination, AppendEntriesArguments message);
        void SendAppendEntriesResult(string destination, AppendEntriesResult message);
        void SendRequestVote(string destination, RequestVoteArguments message);
        void SendRequestVoteResult(string destination, RequestVoteResult message);
    }
}
