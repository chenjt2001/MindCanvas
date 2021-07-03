using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
