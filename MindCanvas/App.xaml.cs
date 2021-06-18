using Microsoft.Services.Store.Engagement;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core.Preview;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace MindCanvas
{
    /// <summary>
    /// 提供特定于应用程序的行为，以补充默认的应用程序类。
    /// </summary>
    sealed partial class App : Application
    {

        // 声明思维导图和文件
        public static MindMap mindMap;
        public static MindCanvasFile mindCanvasFile;

        /// <summary>
        /// 初始化单一实例应用程序对象。这是执行的创作代码的第一行，
        /// 已执行，逻辑上等同于 main() 或 WinMain()。
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.UnhandledException += App_UnhandledException;

            // DEBUG模式则清除所有应用设置
#if DEBUG
            Settings.Clear();
#endif
        }

        /// <summary>
        /// 在应用程序由最终用户正常启动时进行调用。
        /// 将在启动应用程序以打开特定文件等情况下使用。
        /// </summary>
        /// <param name="e">有关启动请求和过程的详细信息。</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            await OnLaunchedOrActivated(e);
        }

        /// <summary>
        /// 导航到特定页失败时调用
        /// </summary>
        ///<param name="sender">导航失败的框架</param>
        ///<param name="e">有关导航失败的详细信息</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// 在将要挂起应用程序执行时调用。  在不知道应用程序
        /// 无需知道应用程序会被终止还是会恢复，
        /// 并让内存内容保持不变。
        /// </summary>
        /// <param name="sender">挂起的请求的源。</param>
        /// <param name="e">有关挂起请求的详细信息。</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: 保存应用程序状态并停止任何后台活动
            deferral.Complete();
        }

        // 文件激活事件
        protected override async void OnFileActivated(FileActivatedEventArgs e)
        {
            await OnLaunchedOrActivated(e);
        }

        // 用户单击通知
        protected override async void OnActivated(IActivatedEventArgs e)
        {
            await OnLaunchedOrActivated(e);
        }

        private async Task OnLaunchedOrActivated(IActivatedEventArgs e)
        {
            // 参考https://stackoverflow.com/questions/41715738/azure-notification-hub-uwp-uwp-toast-notifications-dont-launch-app

            // Initialize things like registering background task before the app is loaded

            Frame rootFrame = Window.Current.Content as Frame;

            StoreServicesEngagementManager engagementManager = StoreServicesEngagementManager.GetDefault();

            // 不要在窗口已包含内容时重复应用程序初始化，
            // 只需确保窗口处于活动状态
            if (rootFrame == null)
            {
                // 初始化，创建新文件
                await EventsManager.NewFile();

                // 设置最小窗口大小
                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(500, 500));

                // 注册推送通知
                await engagementManager.RegisterNotificationChannelAsync();

                // 创建要充当导航上下文的框架，并导航到第一页
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: 从之前挂起的应用程序加载状态
                }

                // 将框架放在当前窗口中
                Window.Current.Content = rootFrame;

                // 退出应用程序事件
                SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += App_CloseRequested;

                // 统计应用使用次数
                Settings.TotalLaunchCount += 1;
            }

            // 通过Toast激活
            if (e is ToastNotificationActivatedEventArgs toastNotificationActivatedEventArgs)
            {
                if (rootFrame.Content == null)
                    rootFrame.Navigate(typeof(MainPage));

                // Action is provided
                if (toastNotificationActivatedEventArgs.Argument.Length > 0)
                {
                    // Obtain the arguments from the notification
                    ToastArguments args = ToastArguments.Parse(toastNotificationActivatedEventArgs.Argument);

                    // 来自合作伙伴中心推送通知的参数
                    string originalArgs = engagementManager.ParseArgumentsAndTrackAppLaunch(toastNotificationActivatedEventArgs.Argument);

                    // Obtain any user input (text boxes, menu selections) from the notification
                    ValueSet userInput = toastNotificationActivatedEventArgs.UserInput;

                    // See what action is being requested
                    if (args.TryGetValue("action", out string value))
                    {
                        if (value == "RequestReviews")
                        {
                            await Windows.System.Launcher.LaunchUriAsync(new Uri(InitialValues.ReviewUri));
                        }
                    }

                    // If we're loading the app for the first time, place the main page on the back stack
                    // so that user can go back after they've been navigated to the specific page
                    if (rootFrame.BackStack.Count == 0)
                        rootFrame.BackStack.Add(new PageStackEntry(typeof(MainPage), null, null));
                }
            }

            // 正常启动
            else if (e is LaunchActivatedEventArgs launchActivatedEventArgs)
            {
                if (launchActivatedEventArgs.PrelaunchActivated == false)
                {
                    if (rootFrame.Content == null)
                    {
                        // 当导航堆栈尚未还原时，导航到第一页，
                        // 并通过将所需信息作为导航参数传入来配置参数
                        rootFrame.Navigate(typeof(MainPage), launchActivatedEventArgs.Arguments);
                    }
                }
            }

            // 文件激活
            else if (e is FileActivatedEventArgs fileActivatedEventArgs)
            {
                // The number of files received is args.Files.Size
                // The name of the first file is args.Files[0].Name
                var file = fileActivatedEventArgs.Files[0] as StorageFile;

                if (mindMap == null)
                    EventsManager.Initialize();

                await EventsManager.OpenFile(file);
                rootFrame.Navigate(typeof(MainPage));
            }

            else
            {
                // TODO: Handle other types of activation
            }

            // 确保当前窗口处于活动状态
            Window.Current.Activate();

            // 显示新功能
            if (SystemInformation.Instance.IsFirstRun || SystemInformation.Instance.IsAppUpdated)
                await Dialog.Show.ShowNewFunction();
        }

        // 退出应用程序
        private void App_CloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            e.Handled = true;
            EventsManager.CloseRequested();
        }

        // 出现未捕获的异常
        private void App_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            LogHelper.Error(e.Message);
        }

        // 在ThemeHelper中用到
        // 来自Xaml-Controls-Gallery
        public static TEnum GetEnum<TEnum>(string text) where TEnum : struct
        {
            if (!typeof(TEnum).GetTypeInfo().IsEnum)
            {
                throw new InvalidOperationException("Generic parameter 'TEnum' must be an enum.");
            }
            return (TEnum)Enum.Parse(typeof(TEnum), text);
        }
    }
}
