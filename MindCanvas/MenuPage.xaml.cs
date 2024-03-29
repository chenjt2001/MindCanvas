﻿using System;
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
        private StorageFile sharedFile;

        public static Frame MenuPageFrame;

        // 上一Frame
        private Microsoft.UI.Xaml.Controls.NavigationViewItem lastItem;

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
            lastItem = OpenItem;

            MenuPageFrame = this.Frame;
        }

        /// <summary>点击导致新项被选中</summary>
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

        /// <summary>导航</summary>
        private void NavView_Navigate(string navItemTag, Windows.UI.Xaml.Media.Animation.NavigationTransitionInfo transitionInfo)
        {
            Type page = null;
            string header = null;

            switch (navItemTag)
            {
                case "settings":
                    page = typeof(SettingsPage);
                    lastItem = NavView.SettingsItem as Microsoft.UI.Xaml.Controls.NavigationViewItem;
                    header = resourceLoader.GetString("Code_Settings");// 设置
                    break;

                case "Help":
                    page = typeof(HelpPage);
                    lastItem = HelpItem;
                    header = resourceLoader.GetString("Code_Help");// 帮助
                    break;

                case "Open":
                    page = typeof(OpenPage);
                    lastItem = OpenItem;
                    header = resourceLoader.GetString("Code_Open");// 打开
                    break;

                case "Encrypt":
                    page = typeof(EncryptPage);
                    lastItem = EncryptItem;
                    header = resourceLoader.GetString("Code_Encrypt");// 加密
                    break;

                case "Export":
                    page = typeof(ExportPage);
                    lastItem = ExportItem;
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

        /// <summary>新建文件</summary>
        private async void NewFile()
        {
            if (await EventsManager.NewFile())
                On_BackRequested();
            else
                NavView.SelectedItem = lastItem;
        }

        /// <summary>保存文件</summary>
        private async void Save()
        {
            if (await EventsManager.SaveFile())
                On_BackRequested();// 保存成功则返回
            else
                NavView.SelectedItem = lastItem;// 没保存，回到上一界面
        }

        /// <summary>分享</summary>
        private async void Share()
        {
            // 生成当前状态的文件
            StorageFolder storageFolder = ApplicationData.Current.TemporaryFolder;
            sharedFile = await storageFolder.CreateFileAsync(
                DateTime.Now.ToLongDateString().ToString() + ".mindcanvas",
                CreationCollisionOption.ReplaceExisting
            );

            MindCanvasFile mindCanvasFile = new MindCanvasFile();
            mindCanvasFile.MindMap = App.mindMap;
            mindCanvasFile.File = sharedFile;
            await mindCanvasFile.SaveFile(addToMru: false);

            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += this.DataRequested;

            DataTransferManager.ShowShareUI();

            NavView.SelectedItem = lastItem;
        }

        /// <summary>DataRequested 事件处理程序，在用户每次调用共享时调用</summary>
        private void DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            if (sharedFile != null)
            {
                List<IStorageItem> storageItems = new List<IStorageItem>();
                storageItems.Add(sharedFile);

                DataRequest request = args.Request;
                request.Data.Properties.Title = sharedFile.Name;
                request.Data.Properties.Description = resourceLoader.GetString("Code_SharedFromMindCanvas");// 已从 MindCanvas 共享
                request.Data.SetStorageItems(storageItems);
            }
        }

        /// <summary>另存为</summary>
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
                NavView.SelectedItem = lastItem;
            }
        }

        /// <summary>用户点击后退按钮</summary>
        private void NavView_BackRequested(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewBackRequestedEventArgs args)
        {
            On_BackRequested();
        }

        /// <summary>Handles system-level BackRequested events and page-level back button Click events</summary>
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