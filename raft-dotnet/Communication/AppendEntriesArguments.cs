using ProtoBuf;

namespace raft_dotnet.Communication
{
    [ProtoContract]
    public class AppendEntriesArguments : RaftMessage
    {
        [ProtoMember(2)]
        public string LeaderId { get; set; }

        [ProtoMember(3)]
        public int PrevLogIndex { get; set; }

        [ProtoMember(4)]
        public int PrevLogTerm { get; set; }

        [ProtoMember(5)]
        public RaftLogEntry[] Entries { get; set; }

        [ProtoMember(6)]
        public int LeaderCommit { get; set; }
    }
}