using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace raft_dotnet
{
    public enum NodeState
    {
        Follower,
        Candidate,
        Leader
    }

    public class RaftNode
    {
        public IRaftRpcClient[] Others { get; set; }
        private NodeState _state;

        private int _currentTerm = 0;
        private string _votedFor = null;
        private IList<RaftLogEntry> _log = new List<RaftLogEntry>();

        private int _commitIndex = 0;
        private int _lastApplied = 0;
        
        public RaftNode(IRaftRpcClient[] others)
        {
            Others = others;
            foreach (var other in others)
            {
                other.Message += (sender, args) => MessageRecieved(args.Message);
            }
        }

        private void MessageRecieved(RaftMessage message)
        {
            if (message.Term > _currentTerm)
            {
                _currentTerm = message.Term;
                _state = NodeState.Follower;
            }
            if (message.Term == _currentTerm)
            {
                // TODO: Process
                throw new NotImplementedException();
            }
        }
        
        /// <summary>
        /// Called when the election timeout is reached.
        /// </summary>
        private async Task BeginElection()
        {
            VoteForSelf();
            foreach (var node in Others)
            {
                var result = await node.RequestVoteAsync(new RequestVoteArguments
                {
                    Term = _currentTerm
                });
            }
        }

        private void VoteForSelf()
        {
            throw new NotImplementedException();
        }
    }
}
