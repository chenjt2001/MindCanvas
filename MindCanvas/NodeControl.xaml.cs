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

        public NodeControl(Node node, MindMap mindMap, bool showAnimation = true)
        {
            this.InitializeComponent();

            this.node = node;
            this.mindMap = mindMap;

            text = node.Name;

            // 边框颜色
            if (node.BorderBrush == null)// 默认
                borderBrush = mindMap.defaultNodeBorderBrush;
            else// 已设置
                borderBrush = node.BorderBrush;

            // 文字大小
            if (node.NameFontSize == 0.0d)// 默认
                fontSize = mindMap.defaultNodeNameFontSize;
            else// 已设置
                fontSize = node.NameFontSize;
        }

        // 背景颜色
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
