using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace raft_dotnet.Tests
{
    [TestClass]
    public class InMemoryTests
    {
        private RaftNode[] _nodes;
        private InMemoryCommunication _communication;

        [TestInitialize]
        public void TestInitializer()
        {
            _communication = new InMemoryCommunication();
            var nodes = new[]
            {
                "localhost:13000",
                "localhost:13001",
                "localhost:13002"
            };

            _nodes = new[]
            {
                new RaftNode(_communication.CreateCommunication(), nodes, "localhost:13000"),
                new RaftNode(_communication.CreateCommunication(), nodes, "localhost:13001"),
                new RaftNode(_communication.CreateCommunication(), nodes, "localhost:13002")
            };
            _communication.Nodes = _nodes;
        }

        [TestMethod]
        public void LeaderElection()
        {
            foreach (var node in _nodes)
            {
                node.Start();
            }
            Thread.Sleep(1000);
            _nodes.Should().ContainSingle(n => n.State == NodeState.Leader);
            _nodes.Where(n => n.State == NodeState.Follower).Should().HaveCount(2);
        }
    }
}
