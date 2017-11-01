namespace raft_dotnet.Communication
{
    public class AppendEntriesArguments : RaftMessage
    {
        public string LeaderId { get; set; }

        public int PrevLogIndex { get; set; }

        public int PrevLogTerm { get; set; }

        public RaftLogEntry[] Entries { get; set; }

        public int LeaderCommit { get; set; }
    }
}