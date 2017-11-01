using System.Threading.Tasks;

namespace raft_dotnet.Communication
{
    public interface IRaftCommunication
    {
        IRaftRpc Server { get; set; }
        Task<AppendEntriesResult> AppendEntriesAsync(string destination, AppendEntriesArguments message);
        Task<RequestVoteResult> RequestVoteAsync(string destination, RequestVoteArguments message);
    }
}