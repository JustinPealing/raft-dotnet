namespace raft_dotnet.Communication
{
    public class RequestVoteArguments : RaftMessage
    {
        public string CandidateId { get; set; }

        public int LastLogIndex { get; set; }

        public int LastLogTerm { get; set; }
    }
}