﻿using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace MindCanvas
{
    static class InfoHelper
    {
        public static InfoBar GetInfoBar(string message, InfoBarSeverity severity = InfoBarSeverity.Informational)
        {
            string title;
            switch (severity)
            {
                case InfoBarSeverity.Error:
                    title = "错误";
                    break;
                case InfoBarSeverity.Success:
                    title = "成功";
                    break;
                case InfoBarSeverity.Warning:
                    title = "警告";
                    break;
                case InfoBarSeverity.Informational:
                default:
                    title = "提示";
                    break;
            }

            InfoBar infoBar = new InfoBar()
            {
                Title = title,
                Severity = severity,
                Message = message
            };

            //infoBar.Margin = new Thickness(100);
            //infoBar.Height = 100;
            //infoBar.Background = new Windows.UI.Xaml.Media.SolidColorBrush(Colors.Red);

            return infoBar;
        }

        // 显示一个InfoBar
        public static void ShowInfoBar(string message, InfoBarSeverity severity = InfoBarSeverity.Informational)
        {
            if (Window.Current.Content is Frame frame && frame.Content is Page page && page.Content is Grid grid)
            {
                if (grid.FindName("InfoBar") is InfoBar infoBar)
                {
                    infoBar.IsOpen = false;
                    grid.Children.Remove(infoBar);
                }

                infoBar = GetInfoBar(message, severity);
                infoBar.VerticalAlignment = VerticalAlignment.Bottom;
                infoBar.Name = "InfoBar";

                EntranceThemeTransition transition = new EntranceThemeTransition();
                infoBar.Transitions.Add(transition);

                Grid.SetRowSpan(infoBar, int.MaxValue);
                Grid.SetColumnSpan(infoBar, int.MaxValue);

                grid.Children.Add(infoBar);

                infoBar.IsOpen = true;
            }
        }
    }
}