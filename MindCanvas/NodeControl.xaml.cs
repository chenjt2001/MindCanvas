using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.UI;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Composition;
using System.Numerics;


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

        // 关于动画
        private Compositor _compositor = Window.Current.Compositor;
        private SpringVector3NaturalMotionAnimation _springAnimation;
        private bool showAnimation;

        private bool isSelected;// 是否处于选中状态

        public NodeControl(Node node, MindMap mindMap, bool showAnimation = true)
        {
            this.InitializeComponent();

            this.node = node;
            this.mindMap = mindMap;
            this.showAnimation = showAnimation;

            text = node.Name;

            // 边框颜色
            if (node.BorderBrush == null)// 默认
                borderBrush = mindMap.defaultNodeBorderBrush;
            else// 已设置
                borderBrush = node.BorderBrush;

            // 文字大小
            if (node.NameFontSize == null)// 默认
                fontSize = mindMap.defaultNodeNameFontSize;
            else// 已设置
                fontSize = node.NameFontSize.Value;

            // 事件
            PointerEntered += NodeControl_PointerEntered;// 鼠标进入
            PointerPressed += NodeControl_PointerPressed;// 鼠标按下
            PointerReleased += NodeControl_PointerReleased;// 鼠标释放
            PointerExited += NodeControl_PointerExited;// 鼠标退出
            RightTapped += NodeControl_RightTapped;
        }

        // 右键或触摸设备长按
        private void NodeControl_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
        }

        // 鼠标释放
        private void NodeControl_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            IsSelected = true;

            if (showAnimation)
            {
                CreateOrUpdateSpringAnimation(1.1f);
                (sender as UIElement).StartAnimation(_springAnimation);
            }
        }

        // 鼠标退出
        private void NodeControl_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (showAnimation)
            {
                if (IsSelected)
                {
                    CreateOrUpdateSpringAnimation(1.05f);
                    (sender as UIElement).StartAnimation(_springAnimation);
                }
                else
                {
                    // Scale back down to 1.0
                    CreateOrUpdateSpringAnimation(1.0f);
                    (sender as UIElement).StartAnimation(_springAnimation);
                }
            }
        }

        // 鼠标按下
        private void NodeControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (showAnimation)
            {
                // 缩放到1.05倍
                CreateOrUpdateSpringAnimation(1.05f);
                StartAnimation(_springAnimation);
            }
        }

        // 鼠标进入
        private void NodeControl_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (showAnimation)
            {
                CreateOrUpdateSpringAnimation(1.1f);
                StartAnimation(_springAnimation);
            }
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
        //public new Brush Background => new SolidColorBrush(Colors.Transparent);
        public new Brush Background => new SolidColorBrush(Colors.White);

        // 边框颜色
        public new Brush BorderBrush
        {
            get
            {
                return borderBrush;
            }
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
        public new Thickness BorderThickness => new Thickness(2);

        // 边框与子对象间的距离
        public new Thickness Padding => new Thickness(10);

        // 圆角半径
        public new CornerRadius CornerRadius => new CornerRadius(5);

        // 文字内容
        public string Text 
        {
            get
            {
                return text;
            }
            set
            {
                if (value != this.text)
                {
                    text = value;
                    NotifyPropertyChanged();
                }
            }
        }

        // 文字大小
        public new double FontSize
        {
            get
            {
                return fontSize;
            }
            set
            {
                if (value != this.fontSize)
                {
                    fontSize = value;
                    NotifyPropertyChanged();
                }
            }
        }

        // 文字颜色
        public new Brush Foreground => new SolidColorBrush(Colors.Black);

        // 选择
        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                isSelected = value;

                if (showAnimation)
                {
                    if (isSelected)
                    {
                        CreateOrUpdateSpringAnimation(1.05f);
                        StartAnimation(_springAnimation);
                    }
                    else
                    {
                        CreateOrUpdateSpringAnimation(1.0f);
                        StartAnimation(_springAnimation);
                    }
                }
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
