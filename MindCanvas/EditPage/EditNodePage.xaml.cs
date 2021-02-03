﻿using System;
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
            mainPage = (MainPage)data["mainPage"];

            // 名称和描述
            NameTextBox.Text = node.Name;
            DescriptionTextBox.Text = node.Description;

            // 边框颜色
            if (node.BorderBrushArgb == null)
                DefaultBorderBrushRadioButton.IsChecked = true;
            else
                CustomBorderBrushRadioButton.IsChecked = true;

            // 字体大小
            if (node.NameFontSize == 0.0d)
                DefaultFontSizeRadioButton.IsChecked = true;
            else
                CustomFontSizeRadioButton.IsChecked = true;
        }

        // 删除点
        private void RemoveNodeBtn_Click(object sender, RoutedEventArgs e)
        {
            EventsManager.RemoveNode(node);
            mainPage.RefreshUnRedoBtn();
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

        // 使显示的点跟随输入框改变
        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            border.Text = NameTextBox.Text;
            border.UpdateLayout();
            Canvas.SetLeft(border, MainPage.mindMapCanvas.Width / 2 + node.X - border.ActualWidth / 2);
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
            if (node.NameFontSize != 0.0d)
            {
                EventsManager.ModifyNodeNameFontSize(node, null);
                mainPage.RefreshUnRedoBtn();
            }
        }

        // 选择自定义字体大小
        private void CustomFontSizeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            FontSizeNumberBox.Value = border.FontSize;

            if (node.NameFontSize == 0.0d)
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
                mainPage.RefreshUnRedoBtn();
            }
        }
    }
}
