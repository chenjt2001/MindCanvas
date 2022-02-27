using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Media;

namespace MindCanvas
{
    /// <summary>思维导图</summary>
    public class MindMap
    {
        private List<Node> nodes;
        private List<Tie> ties;
        private SolidColorBrush defaultNodeBorderBrush;// 默认边框颜色
        private double defaultNodeNameFontSize;// 默认点名称文字大小
        private InkStrokeContainer inkStrokeContainer;// 墨迹
        private double visualCenterX;// 可视中心点X
        private double visualCenterY;// 可视中心点Y
        private float zoomFactor;// 可视区放大倍数
        private string defaultNodeStyle;// 默认点样式
        private SolidColorBrush defaultTieStroke;// 默认线颜色

        // 资源加载器，用于翻译
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

        public float ZoomFactor { get => zoomFactor; set => zoomFactor = value; }
        public double VisualCenterY { get => visualCenterY; set => visualCenterY = value; }
        public double VisualCenterX { get => visualCenterX; set => visualCenterX = value; }
        public InkStrokeContainer InkStrokeContainer { get => inkStrokeContainer; set => inkStrokeContainer = value; }
        public double DefaultNodeNameFontSize { get => defaultNodeNameFontSize; set => defaultNodeNameFontSize = value; }
        public SolidColorBrush DefaultNodeBorderBrush { get => defaultNodeBorderBrush; set => defaultNodeBorderBrush = value; }
        public List<Tie> Ties { get => ties; set => ties = value; }
        public List<Node> Nodes { get => nodes; set => nodes = value; }
        public string DefaultNodeStyle { get => defaultNodeStyle; set => defaultNodeStyle = value; }
        public SolidColorBrush DefaultTieStroke { get => defaultTieStroke; set => defaultTieStroke = value; }

        public MindMap()
        {
            nodes = new List<Node>();
            ties = new List<Tie>();
        }

        /// <summary>初始化成新的思维导图</summary>
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
            defaultNodeStyle = InitialValues.NodeStyle;
            defaultTieStroke = new SolidColorBrush(InitialValues.TieStrokeColor);
        }

        /// <summary>添加点</summary>
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

        /// <summary>添加连接</summary>
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

        /// <summary>获取一条线连接的两个点</summary>
        public List<Node> GetNodes(Tie tie)
        {

            // 按id查找要连接的两个点
            return (from Node node in nodes
                    where node.Id == tie.Node1Id || node.Id == tie.Node2Id
                    select node).ToList();
        }

        /// <summary>获取连接着的点</summary>
        public List<Node> GetNodes(Node node)
        {
            List<Node> nodes;

            nodes = (from Tie tie in GetTies(node)
                     select tie.Node1Id == node.Id ? GetNode(tie.Node2Id) : GetNode(tie.Node1Id)).ToList();

            return nodes;
        }

        /// <summary>获取连着一个点的线有哪些</summary>
        public List<Tie> GetTies(Node node)
        {
            return (from Tie tie in ties
                    where node.Id == tie.Node1Id || node.Id == tie.Node2Id
                    select tie).ToList();
        }

        /// <summary>按Id获取点</summary>
        public Node GetNode(int id)
        {
            return (from Node node in nodes where node.Id == id select node).Single();
        }

        /// <summary>按Id获取线</summary>
        public Tie GetTie(int id)
        {
            return (from Tie tie in ties where tie.Id == id select tie).Single();
        }

        /// <summary>获取两个点间的线</summary>
        public Tie GetTie(Node node1, Node node2)
        {
            return (from Tie tie in ties
                    where (node1.Id == tie.Node1Id && node2.Id == tie.Node2Id) || (node1.Id == tie.Node2Id && node2.Id == tie.Node1Id)
                    select tie).SingleOrDefault();
        }

        /// <summary>删除点</summary>
        public void RemoveNode(Node node)
        {
            // 删除这个点本身
            nodes.Remove(node);

            // 删除关于这个点的连接
            foreach (Tie tie in ties.ToList())
                if (tie.Node1Id == node.Id || tie.Node2Id == node.Id)
                    RemoveTie(tie);
        }

        /// <summary>删除连接</summary>
        public void RemoveTie(Tie tie)
        {
            ties.Remove(tie);
        }

        /// <summary>加载数据并清空当前的数据</summary>
        public void LoadData(MindCanvasFileData mindCanvasFileData)
        {
            this.nodes = mindCanvasFileData.Nodes;
            this.ties = mindCanvasFileData.Ties;

            this.defaultNodeBorderBrush = mindCanvasFileData.DefaultNodeBorderBrush as SolidColorBrush;
            this.defaultNodeNameFontSize = mindCanvasFileData.DefaultNodeNameFontSize;
            this.inkStrokeContainer = mindCanvasFileData.InkStrokeContainer;
            this.zoomFactor = mindCanvasFileData.ZoomFactor;
            this.visualCenterX = mindCanvasFileData.VisualCenterX;
            this.visualCenterY = mindCanvasFileData.VisualCenterY;
            this.defaultNodeStyle = mindCanvasFileData.DefaultNodeStyle;
            this.defaultTieStroke = mindCanvasFileData.DefaultTieStroke as SolidColorBrush;
        }

        /// <summary>获取可序列化的数据</summary>
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
                VisualCenterY = visualCenterY,
                DefaultNodeStyle = defaultNodeStyle,
                DefaultTieStroke = defaultTieStroke
            };

            return mindCanvasFileData;
        }

        /// <summary>修改点名称和描述</summary>
        public Node ModifyNode(int id, string name, string description)
        {
            Node node = GetNode(id);

            node.Name = name;
            node.Description = description;

            return node;
        }

        /// <summary>修改点位置</summary>
        public Node ModifyNode(int id, double x, double y)
        {
            Node node = GetNode(id);

            node.X = x;
            node.Y = y;

            return node;
        }

        /// <summary>修改线</summary>
        public Tie ModifyTie(int id, string description)
        {
            Tie tie = GetTie(id);
            tie.Description = description;

            return tie;
        }
    }
}