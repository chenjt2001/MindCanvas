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

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace MindCanvas
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class DefaultSettingsPage : Page
    {
        public DefaultSettingsPage()
        {
            this.InitializeComponent();
            BorderBrushColorPicker.Color = App.mindMap.defaultNodeBorderBrush.Color;
            FontSizeNumberBox.Value = App.mindMap.defaultNodeNameFontSize;
        }

        // 取消
        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            On_BackRequested();
        }

        // 保存
        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            EventsManager.ModifyDefaultSettings(BorderBrushColorPicker.Color, FontSizeNumberBox.Value);
            On_BackRequested();
        }

        private bool On_BackRequested()
        {
            Frame.GoBack();
            return true;
        }

        // 恢复初始值
        private void InitializeBtn_Click(object sender, RoutedEventArgs e)
        {
            BorderBrushColorPicker.Color = InitialValues.NodeBorderBrushColor;
            FontSizeNumberBox.Value = InitialValues.NodeNameFontSize;
        }

        // 返回
        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {

            // 默认设置已修改
            if (BorderBrushColorPicker.Color != App.mindMap.defaultNodeBorderBrush.Color || 
                FontSizeNumberBox.Value != App.mindMap.defaultNodeNameFontSize)
            {
                ContentDialogResult result = await Dialog.AskForSaveSettings();
                //用户选择取消
                if (result == ContentDialogResult.None)
                    return;

                // 用户选择保存
                if (result == ContentDialogResult.Primary)
                {
                    EventsManager.ModifyDefaultSettings(BorderBrushColorPicker.Color, FontSizeNumberBox.Value);
                    On_BackRequested();
                }

                // 用户选择不保存，直接返回
                else if (result == ContentDialogResult.Secondary)
                {
                    On_BackRequested();
                }
            }

            // 默认设置未修改，直接返回
            else
                On_BackRequested();
        }

        private void FontSizeNumberBox_ValueChanged(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args)
        {
            if (double.IsNaN(FontSizeNumberBox.Value))
            {
                FontSizeNumberBox.Value = App.mindMap.defaultNodeNameFontSize;
            }
        }
    }
}
