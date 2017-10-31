namespace raft_dotnet
{
    public class AppendEntriesArguments : RaftMessage
    {
        public int LeaderId { get; set; }

        public int PrevLogIndex { get; set; }

        public int PrevLogTerm { get; set; }

        public RaftLogEntry[] Entries { get; set; }

        public int LeaderCommit { get; set; }
    }
}