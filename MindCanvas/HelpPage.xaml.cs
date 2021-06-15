using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace MindCanvas
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class HelpPage : Page
    {
        public HelpPage()
        {
            this.InitializeComponent();
        }

        private string Version
        {
            get
            {
                var version = Windows.ApplicationModel.Package.Current.Id.Version;
                string result = string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
#if DEBUG
                result += " (DEBUG Mode)";
#endif
                return result;
            }
        }

        private async void NewFunctionBtn_Click(object sender, RoutedEventArgs e)
        {
            await Dialog.Show.ShowNewFunction();
        }
    }
}
