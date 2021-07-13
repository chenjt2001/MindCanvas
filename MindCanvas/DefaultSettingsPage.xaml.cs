using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
            BorderBrushColorPicker.Color = App.mindMap.DefaultNodeBorderBrush.Color;// 点的默认颜色
            FontSizeNumberBox.Value = App.mindMap.DefaultNodeNameFontSize;// 点的默认大小

            // 点的默认样式
            foreach (RadioButton radioButton in StyleRadioButtons.Items)
                if (radioButton.Tag.ToString() == App.mindMap.DefaultNodeStyle)
                    StyleRadioButtons.SelectedItem = radioButton;

            StrokeColorPicker.Color = App.mindMap.DefaultTieStroke.Color;// 线的默认颜色
        }

        // 取消
        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            On_BackRequested();
        }

        // 保存
        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            Save();
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

            // 点的默认样式
            foreach (RadioButton radioButton in StyleRadioButtons.Items)
                if (radioButton.Tag.ToString() == App.mindMap.DefaultNodeStyle)
                    StyleRadioButtons.SelectedItem = radioButton;

            LogHelper.Debug(StyleRadioButtons.SelectedItem);

            StrokeColorPicker.Color = InitialValues.TieStrokeColor;
        }

        // 返回
        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // 默认设置已修改
            if (BorderBrushColorPicker.Color != App.mindMap.DefaultNodeBorderBrush.Color
                || FontSizeNumberBox.Value != App.mindMap.DefaultNodeNameFontSize
                || (StyleRadioButtons.SelectedItem as RadioButton).Tag.ToString() != App.mindMap.DefaultNodeStyle
                || StrokeColorPicker.Color != App.mindMap.DefaultTieStroke.Color)
            {
                ContentDialogResult result = await Dialog.Show.AskForSaveSettings();
                //用户选择取消
                switch (result)
                {
                    case ContentDialogResult.None:
                        return;
                    case ContentDialogResult.Primary:
                        Save();
                        On_BackRequested();
                        break;
                    case ContentDialogResult.Secondary:
                        On_BackRequested();
                        break;
                }
            }

            // 默认设置未修改，直接返回
            else
                On_BackRequested();
        }

        // 保存
        private void Save()
        {
            EventsManager.ModifyDefaultSettings(BorderBrushColorPicker.Color,
                                                FontSizeNumberBox.Value,
                                                (StyleRadioButtons.SelectedItem as RadioButton).Tag.ToString(),
                                                StrokeColorPicker.Color);
        }

        private void FontSizeNumberBox_ValueChanged(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args)
        {
            if (double.IsNaN(FontSizeNumberBox.Value))
                FontSizeNumberBox.Value = App.mindMap.DefaultNodeNameFontSize;
        }
    }
}
