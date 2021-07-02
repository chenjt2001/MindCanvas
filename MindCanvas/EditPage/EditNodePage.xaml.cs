using System.Collections.Generic;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace MindCanvas.EditPage
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class EditNodePage : Page
    {
        private Node node;
        private NodeControl border;
        private MainPage mainPage;
        private Dictionary<string, object> data;

        public EditNodePage()
        {
            this.InitializeComponent();
            Loaded += EditNodePage_Loaded;
        }

        private void EditNodePage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            data = (Dictionary<string, object>)e.Parameter;
        }

        private void LoadData()
        {
            node = (Node)data["node"];
            border = (NodeControl)data["border"];
            mainPage = MainPage.mainPage;

            // 名称和描述
            NameTextBox.Text = node.Name;
            DescriptionTextBox.Text = node.Description;

            // 边框颜色
            if (node.BorderBrushArgb == null)
                DefaultBorderBrushRadioButton.IsChecked = true;
            else
                CustomBorderBrushRadioButton.IsChecked = true;

            // 字体大小
            if (node.NameFontSize == null)
                DefaultFontSizeRadioButton.IsChecked = true;
            else
                CustomFontSizeRadioButton.IsChecked = true;

            // 父节点
            AsAStandaloneNodeToggleSwitch.IsOn = !node.ParentNodeId.HasValue;

            List<Node> tiedNodes = App.mindMap.GetNodes(node);
            AsAStandaloneNodeToggleSwitch.IsEnabled = tiedNodes.Count != 0;
            ParentNodeTipTextBlock.Text = tiedNodes.Count != 0 ? "父节点必须为于此点连接着的点之一。" : "必须作为独立节点，因为没有与此点连接着的点。";

            foreach (Node tiedNode in tiedNodes)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Tag = tiedNode.Id;
                item.DataContext = tiedNode.Name;
                ParentNodeComboBox.Items.Add(item);
            }
                
        }

        // 删除点
        private void RemoveNodeBtn_Click(object sender, RoutedEventArgs e)
        {
            EventsManager.RemoveNode(node);
            mainPage.RefreshUnRedoBtn();
            mainPage.ShowFrame(typeof(EditPage.InfoPage));
        }

        // 选择默认边框颜色
        private void DefaultBorderBrushRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (node.BorderBrushArgb != null)
            {
                EventsManager.ModifyNodeBorderBrushColor(node, null);
                mainPage.RefreshUnRedoBtn();
            }
        }

        // 选择自定义边框颜色
        private void CustomBorderBrushRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            BorderBrushColorPicker.Color = (border.BorderBrush as SolidColorBrush).Color;

            if (node.BorderBrushArgb == null)
            {
                EventsManager.ModifyNodeBorderBrushColor(node, BorderBrushColorPicker.Color);
                mainPage.RefreshUnRedoBtn();
            }
        }

        // 名称文本框失去焦点
        private void NameTextBox_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            // 如果已修改，则修改点名称
            if (NameTextBox.Text != node.Name)
            {
                EventsManager.ModifyNode(node, NameTextBox.Text, DescriptionTextBox.Text);
                mainPage.RefreshUnRedoBtn();
            }
        }

        // 描述文本框失去焦点
        private void DescriptionTextBox_LosingFocus(object sender, RoutedEventArgs e)
        {
            // 如果已修改，则修改点描述
            if (DescriptionTextBox.Text != node.Description)
            {
                EventsManager.ModifyNode(node, NameTextBox.Text, DescriptionTextBox.Text);
                mainPage.RefreshUnRedoBtn();
            }
        }

        // 使显示的点名称跟随输入框改变
        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            border.Text = NameTextBox.Text;
            border.UpdateLayout();
            RefreshNodePosition();
        }

        // 使点描述提示跟随输入框改变
        private void DescriptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            border.ToolTipContent = DescriptionTextBox.Text;
        }

        // 使边框颜色跟随当前值改变
        private void BorderBrushColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            border.BorderBrush = new SolidColorBrush(BorderBrushColorPicker.Color);
        }

        // 鼠标释放，完成颜色修改
        private void BorderBrushColorPicker_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            if (node.BorderBrushArgb != null)
            {
                if (BorderBrushColorPicker.Color.A != node.BorderBrushArgb[0] ||
                    BorderBrushColorPicker.Color.R != node.BorderBrushArgb[1] ||
                    BorderBrushColorPicker.Color.G != node.BorderBrushArgb[2] ||
                    BorderBrushColorPicker.Color.B != node.BorderBrushArgb[3])
                {
                    EventsManager.ModifyNodeBorderBrushColor(node, BorderBrushColorPicker.Color);
                    mainPage.RefreshUnRedoBtn();
                }
            }
            else
            {
                EventsManager.ModifyNodeBorderBrushColor(node, BorderBrushColorPicker.Color);
                mainPage.RefreshUnRedoBtn();
            }
        }

        // 选择默认字体大小
        private void DefaultFontSizeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (node.NameFontSize != null)
            {
                EventsManager.ModifyNodeNameFontSize(node, null);
                RefreshNodePosition();
                mainPage.RefreshUnRedoBtn();
            }
        }

        // 选择自定义字体大小
        private void CustomFontSizeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            FontSizeNumberBox.Value = border.FontSize;

            if (node.NameFontSize == null)
            {
                EventsManager.ModifyNodeNameFontSize(node, FontSizeNumberBox.Value);
                mainPage.RefreshUnRedoBtn();
            }
        }

        // 自定义字体大小输入完成
        private void FontSizeNumberBox_ValueChanged(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args)
        {

            // 用户输入为空则设置为默认
            if (double.IsNaN(FontSizeNumberBox.Value))
            {
                DefaultFontSizeRadioButton.IsChecked = true;
                return;
            }

            if (FontSizeNumberBox.Value != node.NameFontSize)
            {
                EventsManager.ModifyNodeNameFontSize(node, FontSizeNumberBox.Value);
                RefreshNodePosition();
                mainPage.RefreshUnRedoBtn();
            }
        }

        // 刷新点位置
        private void RefreshNodePosition()
        {
            border.UpdateLayout();
            Canvas.SetLeft(border, MainPage.mindMapCanvas.Width / 2 + node.X - border.ActualWidth / 2);
            Canvas.SetTop(border, MainPage.mindMapCanvas.Height / 2 + node.Y - border.ActualHeight / 2);
        }

        // 按Shift+Enter键才能换行
        private void NameTextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
                if (!Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
                    e.Handled = true;
        }

        // 切换作为独立点开关
        private void AsAStandaloneNodeToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ParentNodeComboBox.Visibility = AsAStandaloneNodeToggleSwitch.IsOn ? Visibility.Collapsed : Visibility.Visible;
            
        }

        // 选择了新的父节点
        private void ParentNodeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}
