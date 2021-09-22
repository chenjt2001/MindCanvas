
using System;
using MindCanvas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using System.Threading.Tasks;

namespace MindCanvas.Tests
{
    [TestClass]
    public class MindMapTests
    {
        private MindMap mindMap;

        [TestInitialize]
        public async Task Initalize()
        {
            await ExecuteOnUIThread(() =>
            {
                mindMap = new MindMap();

                mindMap.Initialize();
            });

            Assert.AreEqual(mindMap.Nodes.Count, 1);
            Assert.AreEqual(mindMap.Nodes[0].Id, 0);
            Assert.AreEqual(mindMap.Ties.Count, 0);
        }

        [TestMethod]
        public void AddNodeTest()
        {
            int n = mindMap.Nodes.Count;
            for (int i = 0; i < 100; i++)
            {
                Node newNode = mindMap.AddNode("AddNodeTest", "None");

                Assert.AreEqual(mindMap.Nodes.Count, n + i + 1);
                Assert.AreEqual(newNode.Name, "AddNodeTest");
                Assert.AreEqual(newNode.Description, "None");
            }
        }

        [TestMethod]
        public void AddTieTest()
        {
            Node node1 = mindMap.AddNode("AddTieTest", "");
            Node node2 = mindMap.AddNode("AddTieTest", "");

            int n = mindMap.Ties.Count;
            Tie newTie = mindMap.AddTie(node1.Id, node2.Id, "None");

            Assert.AreEqual(mindMap.Ties.Count, n + 1);
            Assert.AreEqual(newTie.Description, "None");
        }

        [TestMethod]
        public void GetNodeTest()
        {
            int id = mindMap.AddNode("GetNodeTest", "").Id;

            Assert.AreEqual(mindMap.GetNode(id).Id, id);
        }

        [TestMethod]
        public void GetTieTest()
        {
            Node node1 = mindMap.AddNode("GetTieTest", "");
            Node node2 = mindMap.AddNode("GetTieTest", "");
            Node node3 = mindMap.AddNode("GetTieTest", "");

            Tie tie = mindMap.AddTie(node1.Id, node2.Id, "");

            Assert.AreEqual(mindMap.GetTie(node1, node2), tie);
            Assert.AreEqual(mindMap.GetTie(node1, node3), null);
        }

        [TestMethod]
        public void GetTiesTest()
        {
            Node node1 = mindMap.AddNode("GetTiesTest", "");
            Node node2 = mindMap.AddNode("GetTiesTest", "");
            Node node3 = mindMap.AddNode("GetTiesTest", "");

            Tie tie1 = mindMap.AddTie(node1.Id, node2.Id, "");
            Tie tie2 = mindMap.AddTie(node1.Id, node3.Id, "");

            Assert.AreEqual(mindMap.GetTies(node1).Count, 2);
            Assert.IsTrue(mindMap.GetTies(node1).Contains(tie1));
            Assert.IsTrue(mindMap.GetTies(node1).Contains(tie2));

            Assert.AreEqual(mindMap.GetTies(node2).Count, 1);
            Assert.IsTrue(mindMap.GetTies(node2).Contains(tie1));
        }

        public static IAsyncAction ExecuteOnUIThread(Windows.UI.Core.DispatchedHandler action)
        {
            return Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, action);
        }
    }
}
