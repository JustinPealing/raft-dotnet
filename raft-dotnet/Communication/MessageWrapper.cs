using ProtoBuf;

namespace raft_dotnet.Communication
{
    [ProtoContract]
    public class MessageWrapper
    {
        [ProtoMember(1)]
        public RaftMessage Message { get; set; }
    }
}
