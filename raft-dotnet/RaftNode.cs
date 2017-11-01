using System;
using System.Collections.Generic;
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
        private static readonly Random Rnd = new Random();
        
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

        private void OnMessage(object sender, RaftMessageEventArgs message)
        {
            if (message.Message is RequestVoteArguments requestVoteArguments)
            {
                Communication.SendRequestVoteResult(requestVoteArguments.CandidateId, RequestVote(requestVoteArguments));
            }
            if (message.Message is RequestVoteResult requestVoteResult)
            {
                RequestVoteResponse(requestVoteResult);
            }
            if (message.Message is AppendEntriesArguments appendEntriesArguments)
            {
                Communication.SendAppendEntriesResult(appendEntriesArguments.LeaderId, AppendEntries(appendEntriesArguments));
            }
            if (message.Message is AppendEntriesResult appendEntriesResult)
            {
                AppendEntriesResponse(appendEntriesResult);
            }
        }

        private void SendAppendEntries()
        {
            lock (_lock)
            {
                foreach (var node in _nodes)
                {
                    var request = new AppendEntriesArguments
                    {
                        Term = _currentTerm,
                        LeaderId = NodeName
                    };
                    Communication.SendAppendEntries(node, request);
                }
            }
        }

        private void AppendEntriesResponse(AppendEntriesResult result)
        {
            lock (_lock)
            {
                // TODO Implement me
            }
        }

        /// <summary>
        /// Called when the election timeout is reached.
        /// </summary>
        private void BeginElection()
        {
            lock (_lock)
            {
                Log.Information("Begin Election");
                State = NodeState.Candidate;
                _currentTermVotes = 0;
                RecordVote();
                _votedFor = NodeName;
                foreach (var node in _nodes)
                {
                    var request = new RequestVoteArguments
                    {
                        CandidateId = NodeName,
                        Term = _currentTerm
                    };
                    Communication.SendRequestVote(node, request);
                }
            }
        }

        private void RequestVoteResponse(RequestVoteResult result)
        {
            lock (_lock)
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
        }

        private void RecordVote()
        {
            _currentTermVotes++;
            var majority = Math.Ceiling((_nodes.Length + 1) / 2.0);
            if (_currentTermVotes >= majority)
            {
                Log.Information("Recieved Majority {_currentTermVotes} of {majority}", _currentTermVotes, majority);
                _currentTerm++;
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
                    _currentTerm = arguments.Term;
                    State = NodeState.Follower;
                }
                if (arguments.Term == _currentTerm)
                {
                    ResetElectionTimeout();
                    if (_votedFor == null)
                    {
                        Log.Information("RequestVote from {CandidateId}, Voted yes", arguments.CandidateId);
                        _votedFor = arguments.CandidateId;
                        return new RequestVoteResult
                        {
                            Term = _currentTerm,
                            VoteGranted = true
                        };
                    }
                }

                Log.Information("RequestVote from {CandidateId}, Voted no", arguments.CandidateId);
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
                    _currentTerm = arguments.Term;
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
