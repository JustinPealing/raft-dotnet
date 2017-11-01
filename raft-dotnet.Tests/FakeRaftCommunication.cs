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
        
        public IRaftRpc Server { get; set; }

        public async Task<AppendEntriesResult> AppendEntriesAsync(string destination, AppendEntriesArguments message)
        {
            var communication = _communication.GetCommunication(destination);
            return await communication.Server.AppendEntriesAsync(message);
        }
        
        public async Task<RequestVoteResult> RequestVoteAsync(string destination, RequestVoteArguments message)
        {
            var communication = _communication.GetCommunication(destination);
            return await communication.Server.RequestVoteAsync(message);
        }
    }
}