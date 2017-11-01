using ProtoBuf;

namespace raft_dotnet.Communication
{
    [ProtoContract]
    [ProtoInclude(1, typeof(AppendEntriesArguments))]
    [ProtoInclude(2, typeof(AppendEntriesResult))]
    [ProtoInclude(3, typeof(RequestVoteArguments))]
    [ProtoInclude(4, typeof(RequestVoteResult))]
    public class RaftMessage
    {
        [ProtoMember(1)]
        public int Term { get; set; }
    }
}