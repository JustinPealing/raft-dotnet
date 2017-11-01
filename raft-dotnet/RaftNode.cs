using System;
using System.Collections.Generic;
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

        public string NodeName { get; set; }

        public IRaftCommunication Communication { get; }

        public RaftNode(IRaftCommunication communication, string[] nodes)
        {
            Communication = communication;
            _nodes = nodes;
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

        private void AppendEntriesResponse(AppendEntriesResult result)
        {
            // TODO: Implement me
        }

        /// <summary>
        /// Called when the election timeout is reached.
        /// </summary>
        private void BeginElection()
        {
            Log.Information("Begin Election");

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
            _electionTimeout.Reset();
        }

        public RequestVoteResult RequestVote(RequestVoteArguments arguments)
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
                    Log.Information("RequestVode from {CandidateId}, Voted yes", arguments.CandidateId);
                    _votedFor = arguments.CandidateId;
                    return new RequestVoteResult
                    {
                        Term = _currentTerm,
                        VoteGranted = true
                    };
                }
            }

            Log.Information("RequestVode from {CandidateId}, Voted no", arguments.CandidateId);
            return new RequestVoteResult
            {
                Term = _currentTerm,
                VoteGranted = false
            };
        }

        public AppendEntriesResult AppendEntries(AppendEntriesArguments arguments)
        {
            Log.Information("Recieved AppendEntriesAsync from {LeaderId}", arguments.LeaderId);

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
