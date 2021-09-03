using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace MindCanvas
{
    /// <summary>思维导图Canvas</summary>
    public class MindMapCanvas : Canvas// InfiniteCanvas
    {
        private MindMap mindMap;
        private Dictionary<int, NodeControl> nodeIdBorder = new Dictionary<int, NodeControl>();// 标记点id与border
        private Dictionary<int, Windows.UI.Xaml.Shapes.Path> tieIdPath = new Dictionary<int, Windows.UI.Xaml.Shapes.Path>();// 标记线id与path
        private bool showAnimation;// 是否显示动画
        private TransitionCollection transitionCollection = new TransitionCollection();

        public MindMap MindMap { get => mindMap; set => mindMap = value; }

        public MindMapCanvas(bool showAnimation = true)
        {
            mindMap = new MindMap();
            VerticalAlignment = VerticalAlignment.Center;
            HorizontalAlignment = HorizontalAlignment.Center;
            //Background = new SolidColorBrush(Colors.White);

            // 设置动画
            transitionCollection.Add(new EntranceThemeTransition() { IsStaggeringEnabled = false });
            ShowAnimation = showAnimation;

            // 画布大小
            Width = InitialValues.MindMapCanvasWidth;
            Height = InitialValues.MindMapCanvasHeight;
        }

        public bool ShowAnimation
        {
            get => showAnimation;
            set
            {
                showAnimation = value;

                foreach (NodeControl nodeControl in this.nodeIdBorder.Values)
                    nodeControl.ShowAnimation = value;

                ChildrenTransitions = value ? this.transitionCollection : new TransitionCollection();
            }
        }

        /// <summary>画一个点</summary>
        public void Draw(Node node)
        {
            NodeControl nodeControl = new NodeControl(node, mindMap, showAnimation: showAnimation);

            nodeIdBorder[node.Id] = nodeControl;
            Children.Add(nodeControl);

            Size visualSize = nodeControl.GetVisualSize();

            SetTop(nodeControl, this.Height / 2 + node.Y - visualSize.Height / 2);
            SetLeft(nodeControl, this.Width / 2 + node.X - visualSize.Width / 2);
            nodeControl.UpdateLayout();
        }

        /// <summary>画一条线</summary>
        public void Draw(Tie tie)
        {
            // 获取这条线的两个点
            List<Node> nodes = mindMap.GetNodes(tie);
            Node node1 = nodes[0];
            Node node2 = nodes[1];

            NodeControl nodeControl1 = ConvertNodeToBorder(node1);
            NodeControl nodeControl2 = ConvertNodeToBorder(node2);

            Windows.UI.Xaml.Shapes.Path path = NodeControl.GetPathInCanvas(nodeControl1, nodeControl2);

            path.Stroke = tie.Stroke ?? App.mindMap.DefaultTieStroke;

            SetZIndex(path, -10000);// 确保线在点下面
            Children.Add(path);

            tieIdPath[tie.Id] = path;
        }

        /// <summary>查询border对应的点</summary>
        public Node ConvertBorderToNode(NodeControl border)
        {
            Node requiredNode = new Node();
            int id = 0;

            foreach (int i in nodeIdBorder.Keys)
                if (nodeIdBorder[i] == border)
                {
                    id = i;
                    break;
                }

            foreach (Node node in mindMap.Nodes)
                if (node.Id == id)
                {
                    requiredNode = node;
                    break;
                }

            return requiredNode;
        }

        /// <summary>查询点对应的Border</summary>
        public NodeControl ConvertNodeToBorder(Node node)
        {
            return nodeIdBorder[node.Id];
        }


        /// <summary>查询path对应的线</summary>
        public Tie ConvertPathToTie(Windows.UI.Xaml.Shapes.Path path)
        {
            Tie requiredTie = new Tie();
            int id = 0;

            foreach (int i in tieIdPath.Keys)
                if (tieIdPath[i] == path)
                {
                    id = i;
                    break;
                }

            foreach (Tie tie in mindMap.Ties)
                if (tie.Id == id)
                {
                    requiredTie = tie;
                    break;
                }

            return requiredTie;
        }

        /// <summary>查询线对应的Path</summary>
        public Windows.UI.Xaml.Shapes.Path ConvertTieToPath(Tie tie)
        {
            return tieIdPath[tie.Id];
        }

        /// <summary>绘制全部</summary>
        public void DrawAll()
        {
            // 绘制点
            foreach (Node node in mindMap.Nodes)
                Draw(node);
            UpdateLayout();//渲染canvas，确保线正确获取点的位置
            // 绘制线
            foreach (Tie tie in mindMap.Ties)
                Draw(tie);
        }

        /// <summary>清除点</summary>
        public void Clear(Node node)
        {
            NodeControl border = ConvertNodeToBorder(node);
            Children.Remove(border);
        }

        /// <summary>清除连接</summary>
        public void Clear(Tie tie)
        {
            Windows.UI.Xaml.Shapes.Path path = ConvertTieToPath(tie);
            Children.Remove(path);
        }

        /// <summary>重绘</summary>
        public void ReDraw()
        {
            Children.Clear();
            nodeIdBorder.Clear();
            tieIdPath.Clear();
            DrawAll();
        }

        /// <summary>重绘</summary>
        public void ReDraw(Tie tie)
        {
            Children.Remove(ConvertTieToPath(tie));
            UpdateLayout();// 确保获取到了tie连着的两个node对应的border的ActualWidth和ActualHeight
            Draw(tie);
        }

        /// <summary>重绘线</summary>
        public void ReDrawTies(Node node)
        {
            foreach (Tie tie in mindMap.GetTies(node))
                ReDraw(tie);
        }

        /// <summary>获取边框</summary>
        public Rect BoundingRect
        {
            get
            {
                this.UpdateLayout();

                if (this.Children.Count == 0)
                {
                    return new Rect();
                }

                double bottom = this.Children[0].ActualOffset.Y + this.Children[0].ActualSize.Y;
                double top = this.Children[0].ActualOffset.Y;
                double left = this.Children[0].ActualOffset.X;
                double right = this.Children[0].ActualOffset.X + this.Children[0].ActualSize.X;

                foreach (UIElement element in this.Children)
                {
                    if (element as Windows.UI.Xaml.Shapes.Path != null)
                        continue;

                    if (element.ActualOffset.X < left)
                        left = element.ActualOffset.X;
                    if (element.ActualOffset.Y < top)
                        top = element.ActualOffset.Y;
                    if (element.ActualOffset.X + element.ActualSize.X > right)
                        right = element.ActualOffset.X + element.ActualSize.X;
                    if (element.ActualOffset.Y + element.ActualSize.Y > bottom)
                        bottom = element.ActualOffset.Y + element.ActualSize.Y;
                }

                Rect rect = new Rect()
                {
                    X = left,
                    Y = top,
                    Width = right - left,
                    Height = bottom - top,
                };

                return rect;
            }
        }
    }
}
