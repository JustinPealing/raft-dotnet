using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using raft_dotnet.Communication;
using Serilog;

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
        private readonly Random Rnd = new Random();
        
        private readonly object _lock = new object();
        
        private readonly string[] _nodes;
        public NodeState State { get; private set; }

        private readonly ElectionTimeout _electionTimeout = new ElectionTimeout();
        private readonly ElectionTimeout _appendEntriesTimeout = new ElectionTimeout();

        private int _currentTerm;
        private int _currentTermVotes;
        private string _votedFor;
        private IList<RaftLogEntry> _log = new List<RaftLogEntry>();

        private int _commitIndex = 0;
        private int _lastApplied = 0;

        public string NodeName { get; }

        public IRaftCommunication Communication { get; }

        public int MinEllectionTimeoutMs { get; set; } = 150;

        public int MaxEllectionTimeoutMs { get; set; } = 300;

        public RaftNode(IRaftCommunication communication, string[] nodes, string nodeName)
        {
            Communication = communication;
            _nodes = nodes;
            NodeName = nodeName;
            _electionTimeout.TimeoutReached += (sender, args) => BeginElection();
            _appendEntriesTimeout.TimeoutReached += (sender, args) => SendAppendEntries();
            Communication.Message += OnMessage;
        }

        public IEnumerable<string> OtherNodes => _nodes.Where(n => n != NodeName);

        private void OnMessage(object sender, RaftMessageEventArgs message)
        {
            Task.Run(() =>
            {
                if (message.Message is RequestVoteArguments requestVoteArguments)
                {
                    message.Response = RequestVote(requestVoteArguments);
                }
                if (message.Message is RequestVoteResult requestVoteResult)
                {
                    RequestVoteResponse(requestVoteResult);
                }
                if (message.Message is AppendEntriesArguments appendEntriesArguments)
                {
                    message.Response = AppendEntries(appendEntriesArguments);
                }
            });
        }

        private void SendAppendEntries()
        {
            foreach (var node in OtherNodes)
            {
                AppendEntries(node);
            }
        }

        private async Task AppendEntries(string node)
        {
            var request = new AppendEntriesArguments
            {
                Term = _currentTerm,
                LeaderId = NodeName
            };
            var result = await Communication.AppendEntriesAsync(node, request);
        }
        
        /// <summary>
        /// Called when the election timeout is reached.
        /// </summary>
        private void BeginElection()
        {
            lock (_lock)
            {
                _currentTerm++;
                Log.Information("Begin Election, term {Term}", _currentTerm);
                State = NodeState.Candidate;
                _currentTermVotes = 0;
                RecordVote();
                _votedFor = NodeName;
                foreach (var node in OtherNodes)
                {
                    var request = new RequestVoteArguments
                    {
                        CandidateId = NodeName,
                        Term = _currentTerm
                    };
                    Communication.RequestVoteAsync(node, request);
                }
            }
        }

        private void RequestVoteResponse(RequestVoteResult result)
        {
            lock (_lock)
            {
                if (result.Term > _currentTerm)
                {
                    Log.Information("Term {Term} is greater than my term {CurrentTerm}, resetting to follower.", result.Term, _currentTerm);
                    _currentTerm = result.Term;
                    _votedFor = null;
                    State = NodeState.Follower;
                }
                else if (result.Term == _currentTerm && State == NodeState.Candidate)
                {
                    Log.Information("Recieved vote {Vote} in term {Term}", result.VoteGranted, result.Term);
                    if (result.VoteGranted)
                    {
                        RecordVote();
                    }
                }
            }
        }

        private void RecordVote()
        {
            _currentTermVotes++;
            var majority = Math.Ceiling((_nodes.Length + 1) / 2.0);
            if (_currentTermVotes >= majority)
            {
                Log.Information("Recieved Majority {_currentTermVotes} in term {Term}", _currentTermVotes, _currentTerm);
                State = NodeState.Leader;
                _electionTimeout.Dispose();
                _appendEntriesTimeout.Reset(TimeSpan.FromMilliseconds(50));
                SendAppendEntries();
            }
        }

        public void Start()
        {
            ResetElectionTimeout();
        }

        public RequestVoteResult RequestVote(RequestVoteArguments arguments)
        {
            lock (_lock)
            {
                if (arguments.Term > _currentTerm)
                {
                    Log.Information("Term {Term} is greater than my term {CurrentTerm}, resetting to follower. Candidate: {CandidateId}", arguments.Term, _currentTerm, arguments.CandidateId);
                    _currentTerm = arguments.Term;
                    _votedFor = null;
                    State = NodeState.Follower;
                }
                if (arguments.Term == _currentTerm)
                {
                    ResetElectionTimeout();
                    if (_votedFor == null)
                    {
                        Log.Information("Voted yes for {CandidateId} in term {Term}", arguments.CandidateId, arguments.Term);
                        _votedFor = arguments.CandidateId;
                        return new RequestVoteResult
                        {
                            Term = _currentTerm,
                            VoteGranted = true
                        };
                    }
                }

                Log.Information("Voted no for {CandidateId} in term {Term}", arguments.CandidateId, arguments.Term);
                return new RequestVoteResult
                {
                    Term = _currentTerm,
                    VoteGranted = false
                };
            }
        }

        private void ResetElectionTimeout()
        {
            var timeout = Rnd.Next(MinEllectionTimeoutMs, MaxEllectionTimeoutMs);
            _electionTimeout.Reset(TimeSpan.FromMilliseconds(timeout));
        }

        public AppendEntriesResult AppendEntries(AppendEntriesArguments arguments)
        {
            lock (_lock)
            {
                Log.Verbose("Recieved AppendEntriesAsync from {LeaderId}", arguments.LeaderId);
                if (arguments.Term > _currentTerm)
                {
                    Log.Information("Term {Term} is greater than my term {CurrentTerm}, resetting to follower. Leader: {LeaderId}", arguments.Term, _currentTerm, arguments.LeaderId);
                    _currentTerm = arguments.Term;
                    _votedFor = null;
                    State = NodeState.Follower;
                }
                if (arguments.Term == _currentTerm && State == NodeState.Candidate)
                {
                    Log.Information("Term {Term} equals my term {CurrentTerm}, resetting to follower. Leader: {LeaderId}", arguments.Term, _currentTerm, arguments.LeaderId);
                    _currentTerm = arguments.Term;
                    _votedFor = null;
                    State = NodeState.Follower;
                }
                if (arguments.Term == _currentTerm)
                {
                    ResetElectionTimeout();
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
}
