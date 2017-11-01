using ProtoBuf;

namespace raft_dotnet.Communication
{
    [ProtoContract]
    [ProtoInclude(100, typeof(AppendEntriesArguments))]
    [ProtoInclude(101, typeof(AppendEntriesResult))]
    [ProtoInclude(102, typeof(RequestVoteArguments))]
    [ProtoInclude(103, typeof(RequestVoteResult))]
    public class RaftMessage
    {
        [ProtoMember(1)]
        public int Term { get; set; }
    }
}