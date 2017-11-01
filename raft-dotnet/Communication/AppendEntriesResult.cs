using ProtoBuf;

namespace raft_dotnet.Communication
{
    [ProtoContract]
    public class AppendEntriesResult : RaftMessage
    {
        [ProtoMember(2)]
        public bool Success { get; set; }
    }
}