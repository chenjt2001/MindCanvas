using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;


//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace MindCanvas
{
    public sealed partial class NodeControl : UserControl, INotifyPropertyChanged
    {
        private Node node;
        private MindMap mindMap;
        private string text;
        private Brush borderBrush;
        private double fontSize;
        private string toolTipContent;
        private string style;
        private Thickness borderThickness;
        private CornerRadius cornerRadius;
        private Brush background;

        // 关于动画
        private Compositor _compositor = Window.Current.Compositor;
        private SpringVector3NaturalMotionAnimation _springAnimation;
        private bool isSelected;// 是否处于选中状态

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

            // 事件
            PointerEntered += NodeControl_PointerEntered;// 鼠标进入
            PointerPressed += NodeControl_PointerPressed;// 鼠标按下
            PointerReleased += NodeControl_PointerReleased;// 鼠标释放
            PointerExited += NodeControl_PointerExited;// 鼠标退出
        }

        // 鼠标释放
        private void NodeControl_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            IsSelected = true;

            if (ShowAnimation)
            {
                CreateOrUpdateSpringAnimation(1.1f);
                StartAnimation();
            }
        }

        // 鼠标退出
        private void NodeControl_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (ShowAnimation)
            {
                if (IsSelected)
                {
                    CreateOrUpdateSpringAnimation(1.05f);
                    StartAnimation();
                }
                else
                {
                    // Scale back down to 1.0
                    CreateOrUpdateSpringAnimation(1.0f);
                    StartAnimation();
                }
            }
        }

        // 鼠标按下
        private void NodeControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            DescriptionToolTip.IsEnabled = false;

            if (ShowAnimation)
            {
                // 缩放到1.05倍
                CreateOrUpdateSpringAnimation(1.05f);
                StartAnimation();
            }
        }

        // 鼠标进入
        private void NodeControl_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            DescriptionToolTip.IsEnabled = true;

            if (ShowAnimation)
            {
                CreateOrUpdateSpringAnimation(1.1f);
                StartAnimation();
            }
        }

        private void DescriptionToolTip_Opened(object sender, RoutedEventArgs e)
        {
            DescriptionToolTip.IsOpen = toolTipContent != "";
        }

        // 动画配置
        private void CreateOrUpdateSpringAnimation(float finalValue)
        {
            if (_springAnimation == null)
            {
                _springAnimation = _compositor.CreateSpringVector3Animation();
                _springAnimation.Target = "Scale";
            }
            _springAnimation.FinalValue = new Vector3(finalValue);
        }

        // 背景颜色
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

        // 边框颜色
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

        // 边框粗细
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

        // 边框与子对象间的距离
        public new Thickness Padding => new Thickness(10);

        // 圆角半径
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

        // 文字内容
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

        // 描述
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

        // 文字大小
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

        // 样式
        public new string Style
        {
            get => style;
            set
            {
                if (value != this.style)
                {
                    IsSelected = !IsSelected;

                    style = value;
                    NotifyPropertyChanged();

                    IsSelected = !IsSelected;

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

        // 文字颜色
        public new Brush Foreground => new SolidColorBrush(Colors.Black);

        // 选择
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                isSelected = value;

                if (ShowAnimation)
                {
                    if (isSelected)
                    {
                        CreateOrUpdateSpringAnimation(1.05f);
                        StartAnimation();
                    }
                    else
                    {
                        CreateOrUpdateSpringAnimation(1.0f);
                        StartAnimation();
                    }
                }
            }
        }

        // 锚点（线连着的地方）
        public PointF GetAnchor(double x, double y)
        {
            List<PointF> anchor = new List<PointF>();
            switch (Style)
            {
                case "Style 1":
                default:
                    return new PointF((float)node.X, (float)node.Y);

                case "Style 2":
                    return x > node.X
                        ? new PointF((float)(node.X + this.ActualWidth / 2), (float)(node.Y + this.ActualHeight / 2 - 1.5f))
                        : new PointF((float)(node.X - this.ActualWidth / 2), (float)(node.Y + this.ActualHeight / 2 - 1.5f));
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

        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
