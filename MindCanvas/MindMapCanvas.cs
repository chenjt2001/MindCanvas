using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace MindCanvas
{
    /// <summary>思维导图Canvas</summary>
    public class MindMapCanvas : Canvas// InfiniteCanvas
    {
        private Dictionary<int, NodeControl> nodeIdBorder = new Dictionary<int, NodeControl>();// 标记点id与border
        private Dictionary<int, Windows.UI.Xaml.Shapes.Path> tieIdPath = new Dictionary<int, Windows.UI.Xaml.Shapes.Path>();// 标记线id与path
        private bool showAnimation;// 是否显示动画
        private TransitionCollection transitionCollection = new TransitionCollection();

        public MindMap MindMap { get; set; }
        public HashSet<NodeControl> SelectedNodeControlList { get; set; } = new HashSet<NodeControl>();// 选中的点

        public MindMapCanvas(bool showAnimation = true)
        {
            MindMap = new MindMap();
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
            NodeControl nodeControl = new NodeControl(node, MindMap, showAnimation: showAnimation);

            nodeIdBorder[node.Id] = nodeControl;
            Children.Add(nodeControl);

            Size visualSize = nodeControl.GetVisualSize();

            SetTop(nodeControl, this.Height / 2 + node.Y - visualSize.Height / 2);
            SetLeft(nodeControl, this.Width / 2 + node.X - visualSize.Width / 2);
            nodeControl.UpdateLayout();

            nodeControl.PropertyChanged += NodeControl_PropertyChanged;
        }

        private void NodeControl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NodeControl nodeControl = sender as NodeControl;

            // 自动更新SelectedNodeControlList
            if (e.PropertyName == "State")
            {
                if (nodeControl.State.HasFlag(NodeControlState.Selected))
                    SelectedNodeControlList.Add(nodeControl);
                else
                    SelectedNodeControlList.Remove(nodeControl);
            }
        }

        /// <summary>画一条线</summary>
        public void Draw(Tie tie)
        {
            // 获取这条线的两个点
            List<Node> nodes = MindMap.GetNodes(tie);
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
        public Node ConvertBorderToNode(NodeControl nodeControl)
        {
            int id = (from int i in nodeIdBorder.Keys where nodeIdBorder[i] == nodeControl select i).Single();

            return MindMap.GetNode(id);
        }

        /// <summary>查询点对应的Border</summary>
        public NodeControl ConvertNodeToBorder(Node node)
        {
            return nodeIdBorder[node.Id];
        }

        /// <summary>查询path对应的线</summary>
        public Tie ConvertPathToTie(Windows.UI.Xaml.Shapes.Path path)
        {
            int id = (from int i in tieIdPath.Keys where tieIdPath[i] == path select i).Single();

            return MindMap.GetTie(id);
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
            foreach (Node node in MindMap.Nodes)
                Draw(node);
            UpdateLayout();//渲染canvas，确保线正确获取点的位置
            // 绘制线
            foreach (Tie tie in MindMap.Ties)
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
            foreach (Tie tie in MindMap.GetTies(node))
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
