﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
            UseDefaultBorderBrushToggleSwitch.IsOn = node.BorderBrushArgb == null;

            // 字体大小
            UseDefaultFontSizeToggleSwitch.IsOn = !node.NameFontSize.HasValue;

            // 样式
            UseDefaultStyleToggleSwitch.IsOn = node.Style == null;
        }

        // 删除点
        private void RemoveNodeBtn_Click(object sender, RoutedEventArgs e)
        {
            EventsManager.RemoveNode(node);
            mainPage.RefreshUnRedoBtn();
            mainPage.ShowFrame(typeof(EditPage.InfoPage));
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

        // 自定义字体大小输入完成
        private void FontSizeNumberBox_ValueChanged(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args)
        {

            // 用户输入为空则设置为默认
            if (double.IsNaN(FontSizeNumberBox.Value))
            {
                UseDefaultFontSizeToggleSwitch.IsOn = true;
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

        private void UseDefaultBorderBrushToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ColorPickerViewbox.Visibility = UseDefaultBorderBrushToggleSwitch.IsOn ? Visibility.Collapsed : Visibility.Visible;

            // 默认边框颜色
            if (UseDefaultBorderBrushToggleSwitch.IsOn)
            {
                if (node.BorderBrushArgb != null)
                {
                    EventsManager.ModifyNodeBorderBrushColor(node, null);
                    mainPage.RefreshUnRedoBtn();
                }
            }

            // 自定义边框颜色
            else
            {
                BorderBrushColorPicker.Color = (border.BorderBrush as SolidColorBrush).Color;

                if (node.BorderBrushArgb == null)
                {
                    EventsManager.ModifyNodeBorderBrushColor(node, BorderBrushColorPicker.Color);
                    mainPage.RefreshUnRedoBtn();
                }
            }
        }

        private void UseDefaultFontSizeToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            FontSizeNumberBox.Visibility = UseDefaultFontSizeToggleSwitch.IsOn ? Visibility.Collapsed : Visibility.Visible;

            // 默认字体大小
            if (UseDefaultFontSizeToggleSwitch.IsOn)
            {
                if (node.NameFontSize.HasValue)
                {
                    EventsManager.ModifyNodeNameFontSize(node, null);
                    RefreshNodePosition();
                    mainPage.RefreshUnRedoBtn();
                }
            }

            // 自定义字体大小
            else
            {
                FontSizeNumberBox.Value = border.FontSize;

                if (!node.NameFontSize.HasValue)
                {
                    EventsManager.ModifyNodeNameFontSize(node, FontSizeNumberBox.Value);
                    mainPage.RefreshUnRedoBtn();
                }
            }
        }

        private void UseDefaultStyleToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            StyleRadioButtons.Visibility = UseDefaultStyleToggleSwitch.IsOn ? Visibility.Collapsed : Visibility.Visible;
            
            // 默认样式
            if (UseDefaultStyleToggleSwitch.IsOn)
            {
                if (node.Style != null)
                {
                    EventsManager.ModifyNodeStyle(node, null);
                    mainPage.RefreshUnRedoBtn();
                }
            }

            // 自定义样式
            else
            {
                foreach (RadioButton radioButton in StyleRadioButtons.Items)
                    if (radioButton.Tag.ToString() == border.Style)
                        radioButton.IsChecked = true;
            }
        }

        private void StyleRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            EventsManager.ModifyNodeStyle(node, (sender as RadioButton).Tag.ToString());
            mainPage.RefreshUnRedoBtn();
        }
    }
}
