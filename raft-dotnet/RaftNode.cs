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
    
    public class RaftNode : IRaftRpc
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

        private int[] _nextIndex;
        private int[] _matchIndex;

        public string NodeName { get; }

        public IRaftCommunication Communication { get; }

        public int MinEllectionTimeoutMs { get; set; } = 150;

        public int MaxEllectionTimeoutMs { get; set; } = 300;

        public RaftNode(IRaftCommunication communication, string[] nodes, string nodeName)
        {
            Communication = communication;
            communication.Server = this;
            _nodes = nodes;
            _nextIndex = new int[_nodes.Length];
            _matchIndex = new int[_nodes.Length];
            NodeName = nodeName;
            _electionTimeout.TimeoutReached += (sender, args) => BeginElection();
            _appendEntriesTimeout.TimeoutReached += (sender, args) => SendAppendEntries();
        }

        public IEnumerable<string> OtherNodes => _nodes.Where(n => n != NodeName);
        
        private void SendAppendEntries()
        {
            foreach (var node in OtherNodes)
            {
                Task.Run(() => AppendEntries(node));
            }
        }

        private async Task AppendEntries(string node)
        {
            try
            {
                int index = Array.IndexOf(_nodes, node);
                var prevLog = _log.SingleOrDefault(l => l.Index == _nextIndex[index] - 1);

                if (_nextIndex[index] == _matchIndex[index])
                {
                    var request = new AppendEntriesArguments
                    {
                        Term = _currentTerm,
                        LeaderId = NodeName,
                        PrevLogIndex = prevLog?.Index ?? 0,
                        PrevLogTerm = prevLog?.Term ?? 0,
                        Entries = _log.Where(l => l.Index >= _nextIndex[index]).ToArray(),
                        LeaderCommit = _commitIndex
                    };
                    var result = await Communication.AppendEntriesAsync(node, request);
                }
                else
                {
                    // Don't send actual logs until we know how up-to-date the node is
                    var request = new AppendEntriesArguments
                    {
                        Term = _currentTerm,
                        LeaderId = NodeName,
                        PrevLogIndex = prevLog?.Index ?? 0,
                        PrevLogTerm = prevLog?.Term ?? 0,
                        LeaderCommit = _commitIndex
                    };
                    var result = await Communication.AppendEntriesAsync(node, request);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in RequestVote");
            }
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
                    Task.Run(() => RequestVote(node));
                }
            }
        }

        private async Task RequestVote(string node)
        {
            try
            {
                var request = new RequestVoteArguments
                {
                    CandidateId = NodeName,
                    Term = _currentTerm
                };
                var result = await Communication.RequestVoteAsync(node, request);
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
            catch (Exception ex)
            {
                Log.Error(ex, "Error in RequestVote");
            }
        }
        
        private void RecordVote()
        {
            _currentTermVotes++;
            var majority = Math.Ceiling((_nodes.Length + 1) / 2.0);
            if (_currentTermVotes >= majority)
            {
                ComeToPower();
            }
        }

        private void ComeToPower()
        {
            Log.Information("Recieved Majority {_currentTermVotes} in term {Term}", _currentTermVotes, _currentTerm);
            State = NodeState.Leader;
            _electionTimeout.Dispose();
            _appendEntriesTimeout.Reset(TimeSpan.FromMilliseconds(50));
            for (int i = 0; i < _nextIndex.Length; i++)
            {
                _nextIndex[i] = _log.LastOrDefault()?.Index ?? 0;
            }
            SendAppendEntries();
        }

        public void Start()
        {
            ResetElectionTimeout();
        }
        
        private void ResetElectionTimeout()
        {
            var timeout = Rnd.Next(MinEllectionTimeoutMs, MaxEllectionTimeoutMs);
            _electionTimeout.Reset(TimeSpan.FromMilliseconds(timeout));
        }
        
        public async Task<AppendEntriesResult> AppendEntriesAsync(AppendEntriesArguments request)
        {
            lock (_lock)
            {
                Log.Verbose("Recieved AppendEntriesAsync from {LeaderId}", request.LeaderId);
                if (request.Term > _currentTerm)
                {
                    Log.Information("Term {Term} is greater than my term {CurrentTerm}, resetting to follower. Leader: {LeaderId}", request.Term, _currentTerm, request.LeaderId);
                    _currentTerm = request.Term;
                    _votedFor = null;
                    State = NodeState.Follower;
                }
                if (request.Term == _currentTerm && State == NodeState.Candidate)
                {
                    Log.Information("Term {Term} equals my term {CurrentTerm}, resetting to follower. Leader: {LeaderId}", request.Term, _currentTerm, request.LeaderId);
                    _currentTerm = request.Term;
                    _votedFor = null;
                    State = NodeState.Follower;
                }
                if (request.Term == _currentTerm)
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

        public async Task<RequestVoteResult> RequestVoteAsync(RequestVoteArguments request)
        {
            lock (_lock)
            {
                if (request.Term > _currentTerm)
                {
                    Log.Information("Term {Term} is greater than my term {CurrentTerm}, resetting to follower. Candidate: {CandidateId}", request.Term, _currentTerm, request.CandidateId);
                    _currentTerm = request.Term;
                    _votedFor = null;
                    State = NodeState.Follower;
                }
                if (request.Term == _currentTerm)
                {
                    ResetElectionTimeout();
                    if (_votedFor == null)
                    {
                        Log.Information("Voted yes for {CandidateId} in term {Term}", request.CandidateId, request.Term);
                        _votedFor = request.CandidateId;
                        return new RequestVoteResult
                        {
                            Term = _currentTerm,
                            VoteGranted = true
                        };
                    }
                }

                Log.Information("Voted no for {CandidateId} in term {Term}", request.CandidateId, request.Term);
                return new RequestVoteResult
                {
                    Term = _currentTerm,
                    VoteGranted = false
                };
            }
        }
    }
}
