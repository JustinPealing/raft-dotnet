using ProtoBuf;

namespace raft_dotnet.Communication
{
    [ProtoContract]
    public class RequestVoteArguments : RaftMessage
    {
        [ProtoMember(2)]
        public string CandidateId { get; set; }

        [ProtoMember(3)]
        public int LastLogIndex { get; set; }

        [ProtoMember(4)]
        public int LastLogTerm { get; set; }
    }
}