using ProtoBuf;

namespace raft_dotnet.Communication
{
    [ProtoContract]
    public class RaftLogEntry
    {
        [ProtoMember(1)]
        public int Index { get; set; }

        [ProtoMember(2)]
        public int Term { get; set; }
    }
}