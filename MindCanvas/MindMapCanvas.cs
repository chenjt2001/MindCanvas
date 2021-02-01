﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace MindCanvas
{
    // 思维导图Canvas
    public class MindMapCanvas : Canvas// InfiniteCanvas
    {
        private MindMap mindMap;
        private Dictionary<int, NodeControl> nodeIdBorder = new Dictionary<int, NodeControl>();// 标记点id与border
        private Dictionary<int, Windows.UI.Xaml.Shapes.Path> tieIdPath = new Dictionary<int, Windows.UI.Xaml.Shapes.Path>();// 标记线id与path
        private bool showAnimation;// 是否显示动画

        public MindMapCanvas(bool showAnimation = true)
        {
            mindMap = new MindMap();
            VerticalAlignment = VerticalAlignment.Center;
            HorizontalAlignment = HorizontalAlignment.Center;
            //Background = new SolidColorBrush(Colors.White);

            // 设置动画
            this.showAnimation = showAnimation;
            if (showAnimation)
            {
                TransitionCollection tc = new TransitionCollection() { };
                tc.Add(new EntranceThemeTransition() { IsStaggeringEnabled = true });
                ChildrenTransitions = tc;
            }

            // 画布大小
            Width = 100000;
            Height = 100000;
        }


        // 设置此Canvas对应的思维导图
        public void SetMindMap(MindMap mindMap)
        {
            this.mindMap = mindMap;
        }

        // 获取此Canvas对应的思维导图
        public MindMap GetMindMap()
        {
            return mindMap;
        }

        // 画一个点
        public void Draw(Node node)
        {
            NodeControl border = new NodeControl(node, mindMap, showAnimation: showAnimation);
            SetTop(border, Height / 2 + node.Y);
            SetLeft(border, Width / 2 + node.X);

            Children.Add(border);

            nodeIdBorder[node.Id] = border;
        }

        // 画一条线
        public void Draw(Tie tie)
        {
            // 获取这条线的两个点
            List<Node> nodes = mindMap.GetNodes(tie);
            Node node1 = nodes[0];
            Node node2 = nodes[1];
            NodeControl node1Border = ConvertNodeToBorder(node1);
            NodeControl node2Border = ConvertNodeToBorder(node2);

            // 绘制Path
            SolidColorBrush stroke = new SolidColorBrush(Colors.Gray);
            Windows.UI.Xaml.Shapes.Path path = new Windows.UI.Xaml.Shapes.Path()
            {
                Stroke = stroke,// 线条颜色
                StrokeThickness = 3,// 线条粗细
            };

            // https://docs.microsoft.com/zh-cn/windows/uwp/xaml-platform/move-draw-commands-syntax
            // 移动和绘制命令语法
            // 起点和三次方贝塞尔曲线
            // 三次方贝塞尔曲线命令:C controlPoint1 controlPoint2 endPoint
            // controlPoint1: 曲线的第一个控制点，它决定曲线的起始切线
            // controlPoint2: 曲线的第二个控制点，它决定曲线的结束切线。
            // endPoint: 终结点，绘制曲线将通过的点
            string commands = string.Format("M {0} {1} C {4} {1} {4} {3} {2} {3}",
                Width / 2 + node1.X + node1Border.ActualWidth / 2,
                Height / 2 + node1.Y + node1Border.ActualHeight / 2,
                Width / 2 + node2.X + node2Border.ActualWidth / 2,
               Height / 2 + node2.Y + node2Border.ActualHeight / 2,
                (Width / 2 + node1.X + node1Border.ActualWidth / 2 + Width / 2 + node2.X + node2Border.ActualWidth / 2) / 2);
            var pathData = (Geometry)(XamlBindingHelper.ConvertValue(typeof(Geometry), commands));
            path.Data = pathData;

            SetZIndex(path, -10000);// 确保线在点下面
            Children.Add(path);

            tieIdPath[tie.Id] = path;
        }

        // 查询border对应的点
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

            foreach (Node node in mindMap.nodes)
                if (node.Id == id)
                {
                    requiredNode = node;
                    break;
                }

            return requiredNode;
        }

        // 查询点对应的Border
        public NodeControl ConvertNodeToBorder(Node node)
        {
            return nodeIdBorder[node.Id];
        }


        // 查询path对应的线
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

            foreach (Tie tie in mindMap.ties)
                if (tie.Id == id)
                {
                    requiredTie = tie;
                    break;
                }

            return requiredTie;
        }

        // 查询线对应的Path
        public Windows.UI.Xaml.Shapes.Path ConvertTieToPath(Tie tie)
        {
            return tieIdPath[tie.Id];
        }

        // 绘制全部
        public void DrawAll()
        {
            // 绘制点
            foreach (Node node in mindMap.nodes)
                Draw(node);
            UpdateLayout();//渲染canvas，确保线正确获取点的位置
            // 绘制线
            foreach (Tie tie in mindMap.ties)
                Draw(tie);
        }

        // 清除点
        public void Clear(Node node)
        {
            NodeControl border = ConvertNodeToBorder(node);
            Children.Remove(border);
        }

        // 清除连接
        public void Clear(Tie tie)
        {
            Windows.UI.Xaml.Shapes.Path path = ConvertTieToPath(tie);
            Children.Remove(path);
        }

        // 重绘
        public void ReDraw()
        {
            Children.Clear();
            nodeIdBorder.Clear();
            tieIdPath.Clear();
            DrawAll();
        }

        public void ReDraw(Tie tie)
        {
            Children.Remove(ConvertTieToPath(tie));
            UpdateLayout();// 确保获取到了tie连着的两个node对应的border的ActualWidth和ActualHeight
            Draw(tie);
        }

        // 重绘线
        public void ReDrawTies(Node node)
        {
            foreach (Tie tie in mindMap.GetTies(node))
                ReDraw(tie);
        }

        // 修改线的path
        public void ModifyTiePath(Tie tie, string commands)
        {
            Windows.UI.Xaml.Shapes.Path path = ConvertTieToPath(tie);
            var pathData = (Geometry)(XamlBindingHelper.ConvertValue(typeof(Geometry), commands));
            path.Data = pathData;
            UpdateLayout();
        }
    }
}
