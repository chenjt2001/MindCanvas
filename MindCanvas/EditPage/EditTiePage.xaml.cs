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

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace MindCanvas.EditPage
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class EditTiePage : Page
    {
        private Tie tie;
        private Windows.UI.Xaml.Shapes.Path path;
        private MainPage mainPage;
        private Dictionary<string, object> data;

        public EditTiePage()
        {
            this.InitializeComponent();
            Loaded += EditTiePage_Loaded;
        }

        private void EditTiePage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            data = (Dictionary<string, object>)e.Parameter;
        }

        private void LoadData()
        {
            tie = (Tie)data["tie"];
            path = (Windows.UI.Xaml.Shapes.Path)data["path"];
            mainPage = MainPage.mainPage;

            // 描述
            DescriptionTextBox.Text = tie.Description;
        }

        // 描述文本框失去焦点
        private void DescriptionTextBox_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            // 如果已修改，则修改点描述
            if (DescriptionTextBox.Text != tie.Description)
            {
                EventsManager.ModifyTie(tie, DescriptionTextBox.Text);
                mainPage.RefreshUnRedoBtn();
            }
        }

        private void RemoveTieBtn_Click(object sender, RoutedEventArgs e)
        {
            EventsManager.RemoveTie(tie);
            mainPage.RefreshUnRedoBtn(); 
            mainPage.ShowFrame(typeof(EditPage.InfoPage));
        }
    }
}
