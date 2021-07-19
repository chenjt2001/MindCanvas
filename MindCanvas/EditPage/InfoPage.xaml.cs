﻿using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace MindCanvas.EditPage
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class InfoPage : Page
    {
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
                InfoTextBlock.Text = e.Parameter as string;
        }

        public InfoPage()
        {
            this.InitializeComponent();
        }
    }
}
