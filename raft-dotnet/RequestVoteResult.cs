namespace raft_dotnet
{
    public class RequestVoteResult : RaftMessage
    {
        public bool VoteGranted { get; set; }
    }
}