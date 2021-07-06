using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace MindCanvas.EditPage
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SearchResultPage : Page
    {
        private Dictionary<string, object> data;
        private string queryText;
        private MainPage mainPage;

        public SearchResultPage()
        {
            this.InitializeComponent();
            Loaded += SearchResultPage_Loaded; ;
        }

        private void SearchResultPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            data = (Dictionary<string, object>)e.Parameter;
        }

        private void LoadData()
        {
            queryText = (string)data["QueryText"];
            mainPage = MainPage.mainPage;

            List<Item> result = EventsManager.SearchMindMap(queryText);
            if (result.Count == 0)
                NoResultsFoundTextBlock.Visibility = Visibility.Visible;
            else
            {
                SearchResultListView.Visibility = Visibility.Visible;
                SearchResultListView.ItemsSource = result;
            }
        }

        private void SearchResultListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Item item)
            {
                string[] words = item.Text.Split(new Char[] { ' ', ':' });

                switch (words[0])
                {
                    case "Node":
                        Node node = App.mindMap.GetNode((int)item.Tag);
                        mainPage.ChangeView(node.X, node.Y, null);
                        break;
                    case "Tie":
                        Tie tie = App.mindMap.GetTie((int)item.Tag);
                        List<Node> nodes = App.mindMap.GetNodes(tie);
                        mainPage.ChangeView((nodes[0].X + nodes[1].X) / 2, (nodes[0].Y + nodes[1].Y) / 2, null);
                        break;
                }
            }
        }
    }
}
