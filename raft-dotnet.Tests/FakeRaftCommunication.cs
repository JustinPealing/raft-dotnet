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
        
        public async Task<RequestVoteResult> RequestVoteAsync(string destination, RequestVoteArguments message)
        {
            var communication = _communication.GetCommunication(destination);
            var args = new RaftMessageEventArgs { Message = message };
            communication.OnMessage(args);
            return (RequestVoteResult)args.Response;
        }
        
        public void OnMessage(RaftMessageEventArgs args)
        {
            Message?.Invoke(this, args);
        }
    }
}