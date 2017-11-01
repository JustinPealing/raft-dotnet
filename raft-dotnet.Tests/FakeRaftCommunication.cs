using System;
using System.Threading.Tasks;
using raft_dotnet.Communication;

namespace raft_dotnet.Tests
{
    public class FakeRaftCommunication : IRaftCommunication
    {
        private readonly InMemoryCommunication _communication;

        public FakeRaftCommunication(InMemoryCommunication communication)
        {
            _communication = communication;
        }

        public event EventHandler<RaftMessageEventArgs> Message;
        
        public async Task<AppendEntriesResult> AppendEntriesAsync(string destination, AppendEntriesArguments message)
        {
            var communication = _communication.GetCommunication(destination);
            var args = new RaftMessageEventArgs {Message = message};
            communication.OnMessage(args);
            return (AppendEntriesResult)args.Response;
        }

        public void SendAppendEntriesResult(string destination, AppendEntriesResult message)
        {
            var communication = _communication.GetCommunication(destination);
            communication.OnMessage(new RaftMessageEventArgs { Message = message });
        }

        public void SendRequestVote(string destination, RequestVoteArguments message)
        {
            var communication = _communication.GetCommunication(destination);
            communication.OnMessage(new RaftMessageEventArgs { Message = message });
        }

        public void SendRequestVoteResult(string destination, RequestVoteResult message)
        {
            var communication = _communication.GetCommunication(destination);
            communication.OnMessage(new RaftMessageEventArgs { Message = message });
        }

        public void OnMessage(RaftMessageEventArgs args)
        {
            Message?.Invoke(this, args);
        }
    }
}