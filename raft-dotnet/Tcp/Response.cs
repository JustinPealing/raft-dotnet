namespace raft_dotnet.Tcp
{
    public class Response
    {
        public int CorrelationId { get; set; }

        public object Result { get; set; }
    }
}