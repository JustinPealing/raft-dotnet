using System;
using raft_dotnet.Communication;

namespace raft_dotnet.Tcp
{
    public class TcpRaftCommunication : IRaftCommunication, IDisposable
    {
        private string _listenAddress;

        public TcpRaftCommunication(string listenAddress)
        {
            _listenAddress = listenAddress;
        }

        public event EventHandler<RaftMessageEventArgs> Message;

        public void SendAppendEntries(string destination, AppendEntriesArguments message)
        {
            throw new NotImplementedException();
        }

        public void SendAppendEntriesResult(string destination, AppendEntriesResult message)
        {
            throw new NotImplementedException();
        }

        public void SendRequestVote(string destination, RequestVoteArguments message)
        {
            throw new NotImplementedException();
        }

        public void SendRequestVoteResult(string destination, RequestVoteResult message)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {

        }

        public void Dispose()
        {
        }
    }
}
