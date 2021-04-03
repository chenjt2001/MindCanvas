using System;
using System.Collections.ObjectModel;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace MindCanvas
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class OpenPage : Page
    {
        // 跟踪最近使用的文件和文件夹
        private StorageItemMostRecentlyUsedList mru = StorageApplicationPermissions.MostRecentlyUsedList;

        public OpenPage()
        {
            this.InitializeComponent();

            ConfigFileListView();
        }

        // 配置最近访问的文件列表
        private async void ConfigFileListView()
        {
            StorageFile fileItem;
            ObservableCollection<TextBlock> fileList = new ObservableCollection<TextBlock>();
            TextBlock textBlock;

            // 最近访问的文件列表
            foreach (AccessListEntry entry in mru.Entries)
            {
                try
                {
                    string mruToken = entry.Token;
                    string mruMetadata = entry.Metadata;
                    fileItem = await mru.GetFileAsync(mruToken);

                    if (fileItem != null)
                    {
                        textBlock = new TextBlock()
                        {
                            Text = fileItem.Path,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            TextWrapping = TextWrapping.Wrap,
                        };
                        fileList.Add(textBlock);
                    }
                }
                catch (Exception)
                {// 文件可能已被删除
                }
            }
            FileListView.ItemsSource = fileList;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".mindcanvas");

            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
                if (await EventsManager.OpenFile(file))
                    MenuPage.MenuPageFrame.Navigate(typeof(MainPage));// 加载完成，回到主界面
        }

        private async void FileListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            TextBlock listViewItem = e.ClickedItem as TextBlock;
            string path, mruToken, mruMetadata;
            StorageFile fileItem;

            if (listViewItem != null)
            {
                path = listViewItem.Text;

                foreach (AccessListEntry entry in mru.Entries)
                {
                    mruToken = entry.Token;
                    mruMetadata = entry.Metadata;

                    try
                    {
                        fileItem = await mru.GetFileAsync(mruToken);
                        if (path == fileItem.Path)
                            if (await EventsManager.OpenFile(fileItem))
                                MenuPage.MenuPageFrame.Navigate(typeof(MainPage));
                    }
                    catch (Exception)
                    { }// 文件可能已被删除
                }
            }
        }
    }
}
