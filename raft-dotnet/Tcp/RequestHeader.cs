namespace raft_dotnet.Tcp
{
    public class Request
    {
        public int CorrelationId { get; set; }

        public object Arguments { get; set; }
    }
}
