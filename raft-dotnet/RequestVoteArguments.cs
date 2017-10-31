﻿namespace raft_dotnet
{
    public class RequestVoteArguments : RaftMessage
    {
        public string CandidateId { get; set; }

        public int LastLogIndex { get; set; }

        public int LastLogTerm { get; set; }
    }
}