using System.Threading.Tasks;

namespace raft_dotnet.Communication
{
    public interface IRaftRpc
    {
        Task<AppendEntriesResult> AppendEntriesAsync(AppendEntriesArguments request);
        Task<RequestVoteResult> RequestVoteAsync(RequestVoteArguments request);
    }
}