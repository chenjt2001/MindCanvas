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

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace MindCanvas
{
    public sealed partial class LoadingControl : UserControl
    {
        public LoadingControl()
        {
            this.InitializeComponent();
        }

        public void ShowLoading(string message)
        {
            LoadingGrid.Visibility = Visibility.Visible;
            LoadingProgressRing.IsActive = true;
            LoadingTextBlock.Text = message;
        }

        public void HideLoading()
        {
            LoadingProgressRing.IsActive = false;
            LoadingGrid.Visibility = Visibility.Collapsed;
        }
    }
}
