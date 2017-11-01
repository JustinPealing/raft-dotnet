namespace raft_dotnet.Communication
{
    public class RaftMessageEventArgs
    {
        public RaftMessage Message { get; set; }
        public RaftMessage Response { get; set; }
    }
}