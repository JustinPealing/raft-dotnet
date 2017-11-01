using System.Linq;
using raft_dotnet.Communication;

namespace raft_dotnet.Tests
{
    public class InMemoryCommunication
    {
        public RaftNode[] Nodes { get; set; }
        
        public IRaftCommunication CreateCommunication()
        {
            return new FakeRaftCommunication(this);
        }

        internal FakeRaftCommunication GetCommunication(string destination)
        {
            return (FakeRaftCommunication) Nodes.Single(n => n.NodeName == destination).Communication;
        }
    }
}