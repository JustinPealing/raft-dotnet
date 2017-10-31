using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConsoleApp8;

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

        private int _currentTerm = 0;
        private int _currentTermVotes = 0;
        private string _votedFor = null;
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
                    
                };
                node.AppendEntriesAsync(request).ContinueWith(AppendEntriesResponseAsync);
            }
        }

        private void AppendEntriesResponseAsync(Task<AppendEntriesResult> task)
        {
            AppendEntriesResponse(task.Result);
        }

        private void AppendEntriesResponse(AppendEntriesResult taskResult)
        {

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
                node.RequestVoteAsync(request).ContinueWith(RequestVoteResponseAsync);
            }
        }

        private void RequestVoteResponseAsync(Task<RequestVoteResult> task)
        {
            RequestVoteResponse(task.Result);
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
            if (_currentTermVotes > Math.Ceiling(Others.Length / 2.0))
            {
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
            Console.WriteLine($"{NodeName}: Recieved RequestVote");

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
                    _votedFor = arguments.CandidateId;
                    return new RequestVoteResult
                    {
                        Term = _currentTerm,
                        VoteGranted = true
                    };
                }
            }
            return new RequestVoteResult
            {
                Term = _currentTerm,
                VoteGranted = false
            };
        }

        public async Task<AppendEntriesResult> AppendEntriesAsync(AppendEntriesArguments arguments)
        {
            Console.WriteLine($"{NodeName}: Recieved AppendEntriesAsync");

            if (arguments.Term > _currentTerm)
            {
                _currentTerm = arguments.Term;
                State = NodeState.Follower;
            }
            if (arguments.Term == _currentTerm)
            {
                _electionTimeout.Reset();
            }
            return new AppendEntriesResult
            {
                Term = _currentTerm,
                Success = false,
            };
        }
    }
}
