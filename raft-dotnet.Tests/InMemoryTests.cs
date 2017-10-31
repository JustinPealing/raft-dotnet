using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace raft_dotnet.Tests
{
    [TestClass]
    public class InMemoryTests : IInMemoryTests
    {
        private RaftNode[] _nodes;

        [TestInitialize]
        public void TestInitializer()
        {
            var node1 = new RaftNode(new IRaftRpcClient[]
            {
                new FakeRaftRpcClient(),
                new FakeRaftRpcClient()
            });

            var node2 = new RaftNode(new IRaftRpcClient[]
            {
                new FakeRaftRpcClient(),
                new FakeRaftRpcClient()
            });

            var node3 = new RaftNode(new IRaftRpcClient[]
            {
                new FakeRaftRpcClient(),
                new FakeRaftRpcClient()
            });

            ((FakeRaftRpcClient) node1.Others[0]).Remote = node2;
            ((FakeRaftRpcClient) node1.Others[1]).Remote = node3;

            ((FakeRaftRpcClient)node2.Others[0]).Remote = node1;
            ((FakeRaftRpcClient)node2.Others[1]).Remote = node3;

            ((FakeRaftRpcClient)node3.Others[0]).Remote = node1;
            ((FakeRaftRpcClient)node3.Others[1]).Remote = node2;

            _nodes = new[] {node1, node2, node3};
        }

        [TestMethod]
        public void TestMethod1()
        {
            Assert.IsNotNull(_nodes);
        }
    }
}
