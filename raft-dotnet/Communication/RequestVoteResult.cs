using ProtoBuf;

namespace raft_dotnet.Communication
{
    [ProtoContract]
    public class RequestVoteResult : RaftMessage
    {
        [ProtoMember(2)]
        public bool VoteGranted { get; set; }
    }
}