using System;
using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace MindCanvas
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MenuPage : Page
    {
        // 声明共享的文件
        private StorageFile SharedFile;

        public static Frame MenuPageFrame;

        // 资源加载器
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();


        public MenuPage()
        {
            this.InitializeComponent();
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            // 默认选择打开文件界面
            NavView.SelectedItem = OpenItem;

            MenuPageFrame = this.Frame;
        }

        // 点击导致新项被选中
        private void NavView_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected == true)
            {
                NavView_Navigate("settings", args.RecommendedNavigationTransitionInfo);
            }
            else if (args.SelectedItemContainer != null)
            {
                string navItemTag = args.SelectedItemContainer.Tag.ToString();// 获取点中项的Tag值
                NavView_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
            }
        }

        // 导航
        private void NavView_Navigate(string navItemTag, Windows.UI.Xaml.Media.Animation.NavigationTransitionInfo transitionInfo)
        {
            Type page = null;
            string header = null;

            switch (navItemTag)
            {
                case "settings":
                    page = typeof(SettingsPage);
                    header = resourceLoader.GetString("Code_Settings");// 设置
                    break;

                case "Help":
                    page = typeof(HelpPage);
                    header = resourceLoader.GetString("Code_Help");// 帮助
                    break;

                case "Open":
                    page = typeof(OpenPage);
                    header = resourceLoader.GetString("Code_Open");// 打开
                    break;

                case "Encrypt":
                    page = typeof(EncryptPage);
                    header = resourceLoader.GetString("Code_Encrypt");// 加密
                    break;

                case "Export":
                    page = typeof(ExportPage);
                    header = resourceLoader.GetString("Code_Export");// 导出
                    break;

                case "Save":
                    Save();
                    break;

                case "SaveAs":
                    SaveAs();
                    break;

                case "Share":
                    Share();
                    break;

                case "New":
                    NewFile();
                    break;
            }

            // Get the page type before navigation so you can prevent duplicate
            // entries in the backstack.
            var preNavPageType = ContentFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded.
            if (!(page is null) && !Type.Equals(preNavPageType, page))
            {
                NavView.Header = header;
                ContentFrame.Navigate(page, null, transitionInfo);
            }
        }

        // 新建文件
        private async void NewFile()
        {
            if (await EventsManager.NewFile())
                On_BackRequested();
            else
                NavView.SelectedItem = OpenItem;
        }

        // 保存文件
        private async void Save()
        {
            if (await EventsManager.SaveFile())
                On_BackRequested();// 保存成功则返回
            else
                NavView.SelectedItem = OpenItem;// 没保存，回到打开文件界面
        }

        // 分享
        private async void Share()
        {
            // 生成当前状态的文件
            StorageFolder storageFolder = ApplicationData.Current.TemporaryFolder;
            SharedFile = await storageFolder.CreateFileAsync(
                DateTime.Now.ToLongDateString().ToString() + ".mindcanvas",
                CreationCollisionOption.ReplaceExisting
            );

            MindCanvasFile mindCanvasFile = new MindCanvasFile();
            mindCanvasFile.MindMap = App.mindMap;
            mindCanvasFile.File = SharedFile;
            await mindCanvasFile.SaveFile(addToMru: false);

            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += this.DataRequested;

            DataTransferManager.ShowShareUI();

            NavView.SelectedItem = OpenItem;
        }

        // DataRequested 事件处理程序，在用户每次调用共享时调用
        private void DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            if (SharedFile != null)
            {
                List<IStorageItem> storageItems = new List<IStorageItem>();
                storageItems.Add(SharedFile);

                DataRequest request = args.Request;
                request.Data.Properties.Title = SharedFile.Name;
                request.Data.Properties.Description = resourceLoader.GetString("Code_SharedFromMindCanvas");// 已从 MindCanvas 共享
                request.Data.SetStorageItems(storageItems);
            }
        }

        // 另存为
        private async void SaveAs()
        {
            // 暂时将mindCanvasFile.file设为空，然后发起保存
            StorageFile originalFile = App.mindCanvasFile.File;
            App.mindCanvasFile.File = null;
            if (await EventsManager.SaveFile())
                On_BackRequested();

            else
            {
                App.mindCanvasFile.File = originalFile;
                NavView.SelectedItem = OpenItem;
            }
        }

        // 用户点击后退按钮
        private void NavView_BackRequested(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewBackRequestedEventArgs args)
        {
            On_BackRequested();
        }

        // Handles system-level BackRequested events and page-level back button Click events
        private bool On_BackRequested()
        {
            try// 防止用户已经返回了
            {
                Frame.GoBack();
            }
            catch { }

            return true;
        }
    }
}