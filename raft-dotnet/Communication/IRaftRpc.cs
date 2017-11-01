using System.Threading.Tasks;

namespace raft_dotnet.Communication
{
    public interface IRaftRpc
    {
        Task<AppendEntriesResult> AppendEntriesAsync(AppendEntriesArguments message);
        Task<RequestVoteResult> RequestVoteAsync(RequestVoteArguments message);
    }
}