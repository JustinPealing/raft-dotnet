namespace raft_dotnet
{
    public class AppendEntriesResult : RaftMessage
    {
        public bool Success { get; set; }
    }
}