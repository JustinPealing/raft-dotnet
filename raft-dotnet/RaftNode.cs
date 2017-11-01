using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using raft_dotnet;

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
        public IRaftRpcClient[] Others { get; }
        
        public NodeState State { get; private set; }

        private readonly ElectionTimeout _electionTimeout = new ElectionTimeout();
        private readonly ElectionTimeout _appendEntriesTimeout = new ElectionTimeout();

        private int _currentTerm;
        private int _currentTermVotes;
        private string _votedFor;
        private IList<RaftLogEntry> _log = new List<RaftLogEntry>();

        private int _commitIndex = 0;
        private int _lastApplied = 0;

        public string NodeName { get; set; }

        public RaftNode(IRaftRpcClient[] others)
        {
            _electionTimeout.TimeoutReached += (sender, args) => BeginElection();
            _appendEntriesTimeout.TimeoutReached += (sender, args) => SendAppendEntries();
            Others = others;
        }

        private void SendAppendEntries()
        {
            foreach (var node in Others)
            {
                var request = new AppendEntriesArguments
                {
                    Term = _currentTerm,
                    LeaderId = NodeName
                };
                node.AppendEntriesAsync(request).ContinueWith(task => { AppendEntriesResponse(task.Result); });
            }
        }

        private void AppendEntriesResponse(AppendEntriesResult result)
        {
            // TODO: Implement me
        }

        /// <summary>
        /// Called when the election timeout is reached.
        /// </summary>
        private void BeginElection()
        {
            Console.WriteLine($"{NodeName}: Begin Election");

            _currentTermVotes = 0;
            RecordVote();
            _votedFor = NodeName;
            foreach (var node in Others)
            {
                var request = new RequestVoteArguments
                {
                    CandidateId = NodeName,
                    Term = _currentTerm
                };
                node.RequestVoteAsync(request).ContinueWith(task => { RequestVoteResponse(task.Result); });
            }
        }

        private void RequestVoteResponse(RequestVoteResult result)
        {
            if (result.Term > _currentTerm)
            {
                _currentTerm = result.Term;
                State = NodeState.Follower;
            }
            if (result.Term == _currentTerm)
            {
                if (result.VoteGranted)
                {
                    RecordVote();
                }
            }
        }

        private void RecordVote()
        {
            _currentTermVotes++;
            var majority = Math.Ceiling((Others.Length + 1) / 2.0);
            if (_currentTermVotes >= majority)
            {
                Console.WriteLine($"{NodeName} - Recieved Majority {_currentTermVotes} of {majority}");
                _currentTerm++;
                State = NodeState.Leader;
                _electionTimeout.Dispose();
                _appendEntriesTimeout.Reset(TimeSpan.FromMilliseconds(50));
                SendAppendEntries();
            }
        }

        public void Start()
        {
            _electionTimeout.Reset();
        }

        public async Task<RequestVoteResult> RequestVoteAsync(RequestVoteArguments arguments)
        {
            if (arguments.Term > _currentTerm)
            {
                _currentTerm = arguments.Term;
                State = NodeState.Follower;
            }
            if (arguments.Term == _currentTerm)
            {
                _electionTimeout.Reset();
                if (_votedFor == null)
                {
                    Console.WriteLine($"{NodeName}: RequestVode from {arguments.CandidateId}, Voted yes");
                    _votedFor = arguments.CandidateId;
                    return new RequestVoteResult
                    {
                        Term = _currentTerm,
                        VoteGranted = true
                    };
                }
            }

            Console.WriteLine($"{NodeName}: RequestVode from {arguments.CandidateId}, Voted no");
            return new RequestVoteResult
            {
                Term = _currentTerm,
                VoteGranted = false
            };
        }

        public async Task<AppendEntriesResult> AppendEntriesAsync(AppendEntriesArguments arguments)
        {
            Console.WriteLine($"{NodeName}: Recieved AppendEntriesAsync from {arguments.LeaderId}");

            if (arguments.Term > _currentTerm)
            {
                _currentTerm = arguments.Term;
                State = NodeState.Follower;
            }
            if (arguments.Term == _currentTerm)
            {
                _electionTimeout.Reset();
                // TODO: Implement me
            }
            return new AppendEntriesResult
            {
                Term = _currentTerm,
                Success = false,
            };
        }
    }
}
