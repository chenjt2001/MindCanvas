using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MindCanvas
{
    static class LoadingHelper
    {
        // 显示加载界面
        public static Task ShowLoading(string message)
        {
            if (Window.Current.Content is Frame frame)
            {
                if (frame.Content is Page page)
                {
                    if (page.Content is Grid grid)
                    {
                        if (grid.FindName("LoadingControl") is LoadingControl loadingControl)
                        {
                            loadingControl.ShowLoading(message);
                        }
                        else
                        {
                            loadingControl = new LoadingControl();
                            loadingControl.Name = "LoadingControl";
                            grid.Children.Add(loadingControl);
                            Grid.SetColumnSpan(loadingControl, int.MaxValue);
                            Grid.SetRowSpan(loadingControl, int.MaxValue);
                            loadingControl.ShowLoading(message);
                        }
                    }
                }
            }

            return Task.Run(() => Thread.Sleep(1000));
        }

        // 隐藏加载界面
        public static void HideLoading()
        {
            if (Window.Current.Content is Frame frame)
            {
                if (frame.Content is Page page)
                {
                    if (page.Content is Grid grid)
                    {
                        if (grid.FindName("LoadingControl") is LoadingControl loadingControl)
                        {
                            loadingControl.HideLoading();
                            grid.Children.Remove(loadingControl);
                        }
                    }
                }
            }
        }
    }
}
