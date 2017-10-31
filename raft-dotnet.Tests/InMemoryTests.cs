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

        [TestInitialize]
        public void TestInitializer()
        {
            var node1 = new RaftNode(new IRaftRpcClient[]
            {
                new FakeRaftRpcClient(),
                new FakeRaftRpcClient()
            }) {NodeName = "Node1"};

            var node2 = new RaftNode(new IRaftRpcClient[]
                {
                    new FakeRaftRpcClient(),
                    new FakeRaftRpcClient()
                })
                {NodeName = "Node2"};

            var node3 = new RaftNode(new IRaftRpcClient[]
                {
                    new FakeRaftRpcClient(),
                    new FakeRaftRpcClient()
                })
                {NodeName = "Node3"};

            ((FakeRaftRpcClient) node1.Others[0]).Remote = node2;
            ((FakeRaftRpcClient) node1.Others[1]).Remote = node3;

            ((FakeRaftRpcClient) node2.Others[0]).Remote = node1;
            ((FakeRaftRpcClient) node2.Others[1]).Remote = node3;

            ((FakeRaftRpcClient) node3.Others[0]).Remote = node1;
            ((FakeRaftRpcClient) node3.Others[1]).Remote = node2;

            _nodes = new[] {node1, node2, node3};
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
