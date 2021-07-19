using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private string queryText;
        private MainPage mainPage;

        public SearchResultPage()
        {
            this.InitializeComponent();
        }

        // Class defintion should be provided within the namespace being used, outside of any other classes.
        // These two declarations belong outside of the main page class.
        private ObservableCollection<Item> _result = new ObservableCollection<Item>();
        public ObservableCollection<Item> Result => this._result;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            queryText = e.Parameter as string;
            mainPage = MainPage.mainPage;

            List<Item> result = EventsManager.SearchMindMap(queryText);
            if (result.Count == 0)
                NoResultsFoundTextBlock.Visibility = Visibility.Visible;
            else
            {
                SearchResultListView.Visibility = Visibility.Visible;
                foreach (Item item in result)
                    Result.Add(item);
            }
        }

        private void SearchResultListView_ItemClick(object sender, ItemClickEventArgs e)
        {

            if (e.ClickedItem is Item item)
            {
                string[] words = item.Text.Split(new char[] { ' ', ':' });

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

        private void ItemEditButton_Click(object sender, RoutedEventArgs e)
        {
            Item item = (sender as Button).Tag as Item;

            string[] words = item.Text.Split(new char[] { ' ', ':' });

            switch (words[0])
            {
                case "Node":
                    Node node = App.mindMap.GetNode((int)item.Tag);
                    mainPage.ChangeView(node.X, node.Y, null);
                    mainPage.ShowFrame(typeof(EditPage.EditNodePage), node);
                    break;
                case "Tie":
                    Tie tie = App.mindMap.GetTie((int)item.Tag);
                    List<Node> nodes = App.mindMap.GetNodes(tie);
                    mainPage.ChangeView((nodes[0].X + nodes[1].X) / 2, (nodes[0].Y + nodes[1].Y) / 2, null);
                    mainPage.ShowFrame(typeof(EditPage.EditTiePage), tie);
                    break;
            }
        }
    }
}
