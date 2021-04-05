using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Media;

namespace MindCanvas
{
    // 思维导图
    public class MindMap
    {
        public List<Node> nodes;
        public List<Tie> ties;
        public SolidColorBrush defaultNodeBorderBrush;// 默认边框颜色
        public double defaultNodeNameFontSize;// 默认点名称文字大小
        public InkStrokeContainer inkStrokeContainer;// 墨迹
        public double visualCenterX;// 可视中心点X
        public double visualCenterY;// 可视中心点Y
        public float zoomFactor;// 可视区放大倍数

        // 资源加载器，用于翻译
        private ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

        public MindMap()
        {
            nodes = new List<Node>();
            ties = new List<Tie>();
        }

        // 初始化成新的思维导图
        public void Initialize(bool createFirst = true)
        {
            nodes.Clear();
            ties.Clear();

            if (createFirst)
            {
                Node firstNode = new Node
                {
                    Id = 0,
                    Name = resourceLoader.GetString("Code_MasterNode"),// 主节点
                    Description = resourceLoader.GetString("Code_TheFirstNode"),// 第一个节点
                    X = 0,
                    Y = 0,
                };

                nodes.Add(firstNode);
            }

            defaultNodeBorderBrush = new SolidColorBrush(InitialValues.NodeBorderBrushColor);
            defaultNodeNameFontSize = InitialValues.NodeNameFontSize;
            inkStrokeContainer = new InkStrokeContainer();
            visualCenterX = 0;
            visualCenterY = 0;
            zoomFactor = 1;
        }

        // 添加点
        public Node AddNode(string name, string description)
        {
            Node newNode;

            if (nodes.Count() == 0)
            {
                newNode = new Node
                {
                    Id = 0,
                    Name = name,
                    Description = description,
                    X = 0,
                    Y = 0,
                };
            }
            else
            {
                newNode = new Node
                {
                    Id = nodes.Last().Id + 1,
                    Name = name,
                    Description = description,
                    X = nodes.Last().X + 20,
                    Y = nodes.Last().Y + 20,
                };
            }

            nodes.Add(newNode);
            return newNode;
        }

        // 添加连接
        public Tie AddTie(int node1id, int node2id, string description)
        {
            int id;
            if (ties.Count() == 0)
                id = 0;
            else
                id = ties.Last().Id + 1;

            Tie newTie = new Tie
            {
                Id = id,
                Description = description,
                Node1Id = node1id,
                Node2Id = node2id,
            };

            ties.Add(newTie);
            return newTie;
        }

        // 获取一条线连接的两个点
        public List<Node> GetNodes(Tie tie)
        {
            List<Node> needNodes = new List<Node>();

            // 按id查找要连接的两个点
            foreach (Node node in nodes)
                if (node.Id == tie.Node1Id)
                    needNodes.Add(node);
                else if (node.Id == tie.Node2Id)
                    needNodes.Add(node);

            return needNodes;
        }

        // 获取连着一个点的线有哪些
        public List<Tie> GetTies(Node node)
        {
            List<Tie> needTies = new List<Tie>();
            foreach (Tie tie in ties)
                if (node.Id == tie.Node1Id || node.Id == tie.Node2Id)
                    needTies.Add(tie);
            return needTies;
        }

        // 按Id获取点
        private Node GetNode(int id)
        {
            foreach (Node node in nodes)
                if (node.Id == id)
                    return node;
            return null;
        }

        // 按Id获取线
        private Tie GetTie(int id)
        {
            foreach (Tie tie in ties)
                if (tie.Id == id)
                    return tie;
            return null;
        }

        // 获取两个点间的线
        public Tie GetTie(Node node1, Node node2)
        {
            foreach (Tie tie in ties)
                if ((node1.Id == tie.Node1Id && node2.Id == tie.Node2Id) || (node1.Id == tie.Node2Id && node2.Id == tie.Node1Id))
                    return tie;
            return null;
        }

        // 删除点
        public List<Tie> RemoveNode(Node node)
        {
            // 删除这个点本身
            nodes.Remove(node);

            // 删除关于这个点的连接
            List<Tie> needRemove = new List<Tie>();
            foreach (Tie tie in ties)
                if (tie.Node1Id == node.Id || tie.Node2Id == node.Id)
                    needRemove.Add(tie);

            foreach (Tie tie in needRemove)
                RemoveTie(tie);

            return needRemove;
        }

        // 删除连接
        public void RemoveTie(Tie tie)
        {
            ties.Remove(tie);
        }

        // 加载数据并清空当前的数据
        public void Load(MindCanvasFileData mindCanvasFileData)
        {
            this.nodes = mindCanvasFileData.Nodes;
            this.ties = mindCanvasFileData.Ties;

            this.defaultNodeBorderBrush = mindCanvasFileData.DefaultNodeBorderBrush as SolidColorBrush;
            this.defaultNodeNameFontSize = mindCanvasFileData.DefaultNodeNameFontSize;
            this.inkStrokeContainer = mindCanvasFileData.InkStrokeContainer;
            this.zoomFactor = mindCanvasFileData.ZoomFactor;
            this.visualCenterX = mindCanvasFileData.VisualCenterX;
            this.visualCenterY = mindCanvasFileData.VisualCenterY;
        }

        // 获取可序列化的数据
        public MindCanvasFileData GetData()
        {
            MindCanvasFileData mindCanvasFileData = new MindCanvasFileData
            {
                Nodes = nodes,
                Ties = ties,
                DefaultNodeBorderBrush = defaultNodeBorderBrush,
                DefaultNodeNameFontSize = defaultNodeNameFontSize,
                InkStrokeContainer = inkStrokeContainer,
                ZoomFactor = zoomFactor,
                VisualCenterX = visualCenterX,
                VisualCenterY = visualCenterY
            };

            return mindCanvasFileData;
        }

        // 修改点
        public Node ModifyNode(int id, string name, string description)
        {
            Node node = GetNode(id);

            node.Name = name;
            node.Description = description;

            return node;
        }

        public Node ModifyNode(int id, double x, double y)
        {
            Node node = GetNode(id);

            node.X = x;
            node.Y = y;

            return node;
        }

        // 修改线
        public Tie ModifyTie(int id, string description)
        {
            Tie tie = GetTie(id);
            tie.Description = description;

            return tie;
        }
    }
}