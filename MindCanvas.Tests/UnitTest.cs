
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
                mindMap.AddNode("Test" + i.ToString(), "None");
                Assert.AreEqual(mindMap.Nodes.Count, n + i + 1);
            }
        }

        public static IAsyncAction ExecuteOnUIThread(Windows.UI.Core.DispatchedHandler action)
        {
            return Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, action);
        }
    }
}
