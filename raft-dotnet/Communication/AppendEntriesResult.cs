namespace raft_dotnet.Communication
{
    public class AppendEntriesResult : RaftMessage
    {
        public bool Success { get; set; }
    }
}