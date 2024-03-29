﻿using System;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;


//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace MindCanvas
{
    [Flags]
    public enum NodeControlState
    {
        None = 0b0,
        Selected = 0b1,// 被选中
        Highlighted = 0b10,// 鼠标在上面
        Pressed = 0b100,// 鼠标按下
    }

    public sealed partial class NodeControl : UserControl, INotifyPropertyChanged
    {
        private Node node;
        private MindMap mindMap;
        private string text;
        private Brush borderBrush;
        private double fontSize;
        private string toolTipContent;
        private double toolTipVerticalOffset;
        private string style;
        private Thickness borderThickness;
        private CornerRadius cornerRadius;
        private Brush background;
        private NodeControlState state;


        // 关于动画
        private Compositor _compositor = Window.Current.Compositor;
        private SpringVector3NaturalMotionAnimation _springAnimation;

        public NodeControl(Node node, MindMap mindMap, bool showAnimation = true)
        {
            this.InitializeComponent();

            this.node = node;
            this.mindMap = mindMap;
            this.ShowAnimation = showAnimation;

            text = node.Name;
            ToolTipContent = node.Description;

            // 边框颜色
            if (node.BorderBrush == null)// 默认
                borderBrush = mindMap.DefaultNodeBorderBrush;
            else// 已设置
                borderBrush = node.BorderBrush;

            // 文字大小
            if (node.NameFontSize == null)// 默认
                fontSize = mindMap.DefaultNodeNameFontSize;
            else// 已设置
                fontSize = node.NameFontSize.Value;

            // 样式
            if (node.Style == null)
                Style = mindMap.DefaultNodeStyle;
            else
                Style = node.Style;
        }

        private void NodeControl_Loaded(object sender, RoutedEventArgs e)
        {
            // 初始化CenterPoint
            Windows.Foundation.Size visualSize = GetVisualSize();
            CenterPoint = new Vector3
            {
                X = (float)(visualSize.Width / 2),
                Y = (float)(visualSize.Height / 2),
            };
        }

        private void DescriptionToolTip_Opened(object sender, RoutedEventArgs e)
        {
            DescriptionToolTip.IsOpen = toolTipContent != "";
            DescriptionToolTip.PlacementTarget = this;

            DescriptionToolTip.UpdateLayout();

            ToolTipVerticalOffset = -60 - DescriptionToolTip.DesiredSize.Height;
        }

        /// <summary>动画配置</summary>
        private void CreateOrUpdateSpringAnimation(float finalValue)
        {
            if (_springAnimation == null)
            {
                _springAnimation = _compositor.CreateSpringVector3Animation();
                _springAnimation.Target = "Scale";
            }
            _springAnimation.FinalValue = new Vector3(finalValue);
        }

        /// <summary>背景颜色</summary>
        public new Brush Background
        {
            get => background;
            set
            {
                if (value != this.background)
                {
                    background = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>边框颜色</summary>
        public new Brush BorderBrush
        {
            get => borderBrush;
            set
            {
                if (value != this.borderBrush)
                {
                    borderBrush = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>边框粗细</summary>
        public new Thickness BorderThickness
        {
            get => borderThickness;
            set
            {
                if (value != borderThickness)
                {
                    borderThickness = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>边框与子对象间的距离</summary>
        public new Thickness Padding => new Thickness(10);

        /// <summary>圆角半径</summary>
        public new CornerRadius CornerRadius
        {
            get => cornerRadius;
            set
            {
                if (value != cornerRadius)
                {
                    cornerRadius = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>文字内容</summary>
        public string Text
        {
            get => text;
            set
            {
                if (value != this.text)
                {
                    text = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>描述</summary>
        public string ToolTipContent
        {
            get => toolTipContent;
            set
            {
                if (value != this.toolTipContent)
                {
                    toolTipContent = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>描述位置</summary>
        public double ToolTipVerticalOffset
        {
            get => toolTipVerticalOffset;
            set
            {
                if (value != this.toolTipVerticalOffset)
                {
                    toolTipVerticalOffset = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>文字大小</summary>
        public new double FontSize
        {
            get => fontSize;
            set
            {
                if (value != this.fontSize)
                {
                    fontSize = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>样式</summary>
        public new string Style
        {
            get => style;
            set
            {
                if (value != this.style)
                {
                    bool needBeSelected = false;
                    if (State.HasFlag(NodeControlState.Selected))
                    {
                        State &= ~NodeControlState.Selected;//IsSelected = false;
                        needBeSelected = true;
                    }

                    style = value;
                    NotifyPropertyChanged();

                    if (needBeSelected)
                        State |= NodeControlState.Selected;//IsSelected = true;

                    switch (this.style)
                    {
                        case "Style 1":
                            BorderThickness = new Thickness(2);
                            CornerRadius = new CornerRadius(5);
                            Background = new SolidColorBrush(Colors.White);
                            break;

                        case "Style 2":
                            BorderThickness = new Thickness(0, 0, 0, 3);
                            CornerRadius = new CornerRadius(0);
                            Background = new SolidColorBrush(Colors.Transparent);
                            break;
                    }
                }
            }
        }

        /// <summary>文字颜色</summary>
        public new Brush Foreground => new SolidColorBrush(Colors.Black);

        /// <summary>状态</summary>
        public NodeControlState State
        {
            get => state;
            set
            {
                if (state == value)
                    return;

                state = value;

                NotifyPropertyChanged();

                if (!ShowAnimation)
                    return;

                if (state.HasFlag(NodeControlState.Pressed))
                {
                    //NodeDropShadowPanel.ShadowOpacity = 0.5;
                    // 缩放到1.05倍
                    CreateOrUpdateSpringAnimation(1.05f);
                    StartAnimation();
                }
                else if (state.HasFlag(NodeControlState.Highlighted))
                {
                    //NodeDropShadowPanel.ShadowOpacity = 0.5;
                    CreateOrUpdateSpringAnimation(1.1f);
                    StartAnimation();
                }
                else if (state.HasFlag(NodeControlState.Selected))
                {
                    //NodeDropShadowPanel.ShadowOpacity = 0.5;
                    //this.BorderBrush = new RevealBorderBrush();
                    CreateOrUpdateSpringAnimation(1.05f);
                    StartAnimation();
                }
                else
                {
                    //NodeDropShadowPanel.ShadowOpacity = 0;
                    CreateOrUpdateSpringAnimation(1.0f);
                    StartAnimation();
                }
            }
        }

        public bool ShowAnimation { get; set; }

        private void StartAnimation()
        {
            switch (this.Style)
            {
                case "Style 1":
                default:
                    this.StartAnimation(_springAnimation);
                    break;
                case "Style 2":
                    NodeTextBlock.StartAnimation(_springAnimation);
                    break;
            }
        }

        public new Vector3 CenterPoint
        {
            get => base.CenterPoint;
            set
            {
                base.CenterPoint = value;
                NodeTextBlock.CenterPoint = value;
            }
        }

        public Windows.Foundation.Size GetVisualSize()
        {
            // 在UpdateLayout前获取大小
            this.Measure(new Windows.Foundation.Size(double.MaxValue, double.MaxValue));
            Windows.Foundation.Size visualSize = this.DesiredSize;
            this.Arrange(new Windows.Foundation.Rect(new Windows.Foundation.Point(0, 0), visualSize));
            return visualSize;
        }

        /// <summary>锚点（线连着的地方）</summary>
        private static Windows.Foundation.Point GetAnchorInCanvas(NodeControl nodeControl, double anotherX, double anotherY)
        {
            Windows.Foundation.Size visualSize = nodeControl.GetVisualSize();

            double x, y;
            double left = Canvas.GetLeft(nodeControl);
            double top = Canvas.GetTop(nodeControl);

            switch (nodeControl.Style)
            {
                case "Style 1":
                default:
                    x = left + visualSize.Width / 2;
                    y = top + visualSize.Height / 2;
                    break;

                case "Style 2":
                    x = anotherX > left + visualSize.Width / 2 ? left + visualSize.Width : left;
                    y = top + visualSize.Height - 1.5;
                    break;
            }
            return new Windows.Foundation.Point(x, y);
        }

        public static Windows.UI.Xaml.Shapes.Path GetPathInCanvas(NodeControl nodeControl1, NodeControl nodeControl2)
        {
            Canvas canvas = nodeControl1.Parent as Canvas;

            Windows.Foundation.Point anchor1 = GetAnchorInCanvas(nodeControl1, canvas.Width / 2 + nodeControl2.node.X, canvas.Height / 2 + nodeControl2.node.Y);
            Windows.Foundation.Point anchor2 = GetAnchorInCanvas(nodeControl2, canvas.Width / 2 + nodeControl1.node.X, canvas.Height / 2 + nodeControl1.node.Y);

            return PathHelper.NewPath(anchor1.X, anchor1.Y, anchor2.X, anchor2.Y);
        }

        public static void ModifyPathInCanvas(Windows.UI.Xaml.Shapes.Path path, NodeControl nodeControl1, NodeControl nodeControl2)
        {
            double left1 = Canvas.GetLeft(nodeControl1);
            double left2 = Canvas.GetLeft(nodeControl2);
            double top1 = Canvas.GetTop(nodeControl1);
            double top2 = Canvas.GetTop(nodeControl2);

            Windows.Foundation.Point anchor1 = GetAnchorInCanvas(nodeControl1, left2 + nodeControl2.ActualWidth / 2, top2 + nodeControl2.ActualHeight / 2);
            Windows.Foundation.Point anchor2 = GetAnchorInCanvas(nodeControl2, left1 + nodeControl1.ActualWidth / 2, top1 + nodeControl1.ActualHeight / 2);

            PathHelper.ModifyPath(path, anchor1.X, anchor1.Y, anchor2.X, anchor2.Y);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// This method is called by the Set accessor of each property.  
        /// The CallerMemberName attribute that is applied to the optional propertyName
        /// parameter causes the property name of the caller to be substituted as an argument.
        /// </summary>
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
