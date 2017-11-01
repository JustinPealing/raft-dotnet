namespace raft_dotnet.Communication
{
    public class RequestVoteResult : RaftMessage
    {
        public bool VoteGranted { get; set; }
    }
}