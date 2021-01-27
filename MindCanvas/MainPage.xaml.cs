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

using Windows.UI.Xaml.Media.Animation;
using Windows.UI;
using Windows.UI.Composition;
using System.Numerics;
using Windows.Storage.AccessCache;
using Windows.UI.Core.Preview;
using Microsoft.Graphics.Canvas;


// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace MindCanvas
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Compositor _compositor = Window.Current.Compositor;
        private SpringVector3NaturalMotionAnimation _springAnimation;


        private Node nowNode;// 正在操作的点
        private List<Border> selectedBorderList = new List<Border>();// 选中的点
        private Node nowPressedNode; // 正在按着的点
        private bool isMovingNode = false;// 是否有点正在被移动
        public static MindMapCanvas mindMapCanvas;
        public static MainPage mainPage;
        private StorageItemMostRecentlyUsedList mru = StorageApplicationPermissions.MostRecentlyUsedList;
        private ScaleTransform scaleTransform = new ScaleTransform();// 缩放

        public MainPage()
        {
            this.InitializeComponent();

            mainPage = this;

            // 这两个事件用来实现移动点
            PointerMoved += MainPage_PointerMoved;// 鼠标移动事件
            PointerReleased += MainPage_PointerReleased;// 鼠标释放事件

            // 设置缓存
            NavigationCacheMode = NavigationCacheMode.Required;

            RefreshTheme();

            RefreshUnRedoBtn();

            EditFrame.Navigate(typeof(EditPage.InfoPage), null, new DrillInNavigationTransitionInfo()); 
        }

        // 刷新主题设置
        public void RefreshTheme()
        {
            // 应用主题颜色
            if (ThemeHelper.ActualTheme == ElementTheme.Light)
            {
                MenuBtnBorder.Background = new SolidColorBrush(Colors.White);
                AppBarButtonsBorder.Background = new SolidColorBrush(Colors.White);
                AppNameBorder.Background = new SolidColorBrush(Colors.White);
            }
            else if (ThemeHelper.ActualTheme == ElementTheme.Dark)
            {
                MenuBtnBorder.Background = new SolidColorBrush(Colors.Black);
                AppBarButtonsBorder.Background = new SolidColorBrush(Colors.Black);
                AppNameBorder.Background = new SolidColorBrush(Colors.Black);
            }
        }

        // 配置点Border
        private void ConfigNodesBorder(List<Node> needConfig = null)
        {
            if (needConfig == null)
                needConfig = App.mindMap.nodes;

            foreach (Node node in needConfig)
            {
                Border border = mindMapCanvas.ConvertNodeToBorder(node);
                border.PointerPressed += this.Node_Pressed;// 鼠标按下
                border.PointerReleased += this.Node_Released;// 鼠标释放
                border.PointerEntered += this.Node_PointerEntered;// 鼠标进入
                border.PointerExited += this.Node_PointerExited;// 鼠标退出
                border.RightTapped += this.Node_RightTapped;// 右键或触摸设备长按

            }
        }

        // 配置线Path
        private void ConfigTiesPath(List<Tie> needConfig = null)
        {
            if (needConfig == null)
                needConfig = App.mindMap.ties;

            foreach (Tie tie in needConfig)
            {
                Windows.UI.Xaml.Shapes.Path path = mindMapCanvas.ConvertTieToPath(tie);
            }
        }

        // 鼠标在点内释放，算按了一下点
        private void Node_Released(object sender, PointerRoutedEventArgs e)
        {
            Border nowNodeBorder = sender as Border;
            nowNode = mindMapCanvas.ConvertBorderToNode(nowNodeBorder);// 记为nowNode

            // 正在选择点
            if ((bool)TieBtn.IsChecked)
            {
                // 检查点是否已被选择
                if (selectedBorderList.Contains(nowNodeBorder))
                {
                    // 已被选择，现在又点了一次，所以取消选择
                    CreateOrUpdateSpringAnimation(1.0f);
                    nowNodeBorder.StartAnimation(_springAnimation);
                    selectedBorderList.Remove(nowNodeBorder);
                    return;
                }

                // 此点未被选择，现在进行选择
                CreateOrUpdateSpringAnimation(1.05f);
                nowNodeBorder.StartAnimation(_springAnimation);
                selectedBorderList.Add(nowNodeBorder);// 把当前选择的点计入selectedBorderList

                // 如果已经选好了两个点，就连接
                if (selectedBorderList.Count() == 2)
                {
                    Node node1, node2;
                    node1 = mindMapCanvas.ConvertBorderToNode(selectedBorderList[0]);
                    node2 = mindMapCanvas.ConvertBorderToNode(selectedBorderList[1]);

                    // 如果这两个点已经连了线就删除
                    Tie tie = App.mindMap.GetTie(node1, node2);
                    if (tie != null)
                        EventsManager.RemoveTie(tie);
                    // 否则添加
                    else
                        EventsManager.AddTie(node1, node2);

                    RefreshUnRedoBtn();

                    // 不论如何，让这2个被选中的点恢复
                    CreateOrUpdateSpringAnimation(1.0f);
                    foreach (Border border in selectedBorderList)
                        border.StartAnimation(_springAnimation);
                    selectedBorderList.Clear();

                    // 恢复正常
                    TieBtn.IsChecked = false;
                    EditFrame.Navigate(typeof(EditPage.InfoPage), null, new DrillInNavigationTransitionInfo());
                }
            }

            // 没在选择点，查看点信息
            else
            {
                // 缩放到1.1倍
                CreateOrUpdateSpringAnimation(1.1f);
                nowNodeBorder.StartAnimation(_springAnimation);

                // 显示信息
                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    { "node", nowNode },
                    { "border", nowNodeBorder },
                    { "mainPage", this }
                };
                EditFrame.Navigate(typeof(EditPage.EditNodePage), data, new DrillInNavigationTransitionInfo());
            }
        }

        // 鼠标释放
        private void MainPage_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            // 如果有个点在鼠标释放时处于移动状态，就算进行了一次“移动点”
            // 之所以不写在Node_Released里面，是因为可能用户在移动点时鼠标移动太快，导致鼠标未在点内释放
            if (isMovingNode)
            {
                Border border = mindMapCanvas.ConvertNodeToBorder(nowPressedNode);
                var left = (double)border.GetValue(Canvas.LeftProperty);
                var top = (double)border.GetValue(Canvas.TopProperty);

                EventsManager.ModifyNode(nowPressedNode, left, top);
                RefreshUnRedoBtn();
                isMovingNode = false;
            }
            nowPressedNode = null;// 没有点被按下，之所以这句话不写在Node_Released里面
                                  // 是因为当鼠标释放时Node_Released会比MainPage_PointerReleased先运行
                                  // 那MainPage_PointerReleased就不能获取nowPressedNode了
        }

        // 鼠标按下border
        private void Node_Pressed(object sender, PointerRoutedEventArgs e)
        {
            Border border = sender as Border;

            // 缩放到1.05倍
            CreateOrUpdateSpringAnimation(1.05f);
            (border).StartAnimation(_springAnimation);

            nowPressedNode = mindMapCanvas.ConvertBorderToNode(border);
        }

        // 鼠标移动事件
        private void MainPage_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            // 如果这时有个点正在被按下，就移动它
            // 之所以不使用检测“在点border内鼠标移动”的方式来处理这个
            // 是因为在拖动点时可能鼠标移动太快导致丢失点border
            if (nowPressedNode != null)
            {
                Border border = mindMapCanvas.ConvertNodeToBorder(nowPressedNode);
                var point = e.GetCurrentPoint(border);// 获取相对于border的指针
                var pos = point.Position;
                pos.X = pos.X - border.ActualWidth / 2.0;
                pos.Y = pos.Y - border.ActualHeight / 2.0;

                var left = (double)border.GetValue(Canvas.LeftProperty);
                var top = (double)border.GetValue(Canvas.TopProperty);
                border.SetValue(Canvas.LeftProperty, left + pos.X);
                border.SetValue(Canvas.TopProperty, top + pos.Y);

                // 保持线连着点
                foreach (Tie tie in App.mindMap.GetTies(nowPressedNode))
                {
                    List<Node> nodes = App.mindMap.GetNodes(tie);
                    Border anotherBorder;
                    Node anotherNode;
                    if (nodes[0] != nowPressedNode)
                        anotherNode = nodes[0];
                    else
                        anotherNode = nodes[1];
                    anotherBorder = mindMapCanvas.ConvertNodeToBorder(anotherNode);

                    string commands = string.Format("M {0} {1} C {4} {1} {4} {3} {2} {3}",
                        left + pos.X + border.ActualWidth / 2,
                        top + pos.Y + border.ActualHeight / 2,
                        mindMapCanvas.Width / 2 + anotherNode.x + anotherBorder.ActualWidth / 2,
                        mindMapCanvas.Height / 2 + anotherNode.y + anotherBorder.ActualHeight / 2,
                        (left + pos.X + border.ActualWidth / 2 + mindMapCanvas.Width / 2 + anotherNode.x + anotherBorder.ActualWidth / 2) / 2);
                    mindMapCanvas.ModifyTiePath(tie, commands);
                }

                isMovingNode = true;

                // 不用现在提交EventManager，因为鼠标还没释放（nowPressedNode != null）
            }
        }

        // 动画配置
        private void CreateOrUpdateSpringAnimation(float finalValue)
        {
            if (_springAnimation == null)
            {
                _springAnimation = _compositor.CreateSpringVector3Animation();
                _springAnimation.Target = "Scale";
            }
            _springAnimation.FinalValue = new Vector3(finalValue);
        }

        // 鼠标进入border
        private void Node_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            CreateOrUpdateSpringAnimation(1.1f);
            (sender as UIElement).StartAnimation(_springAnimation);
        }

        // 鼠标退出border
        private void Node_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            // 如果这个点正在被选择，就不显示退出的动画，而是只缩小一点点
            if (selectedBorderList.Contains(sender as Border))
            {
                CreateOrUpdateSpringAnimation(1.05f);
                (sender as UIElement).StartAnimation(_springAnimation);
            }

            else
            {
                // Scale back down to 1.0
                CreateOrUpdateSpringAnimation(1.0f);
                (sender as UIElement).StartAnimation(_springAnimation);
            }
        }

        // 右键或鼠标长按
        private void Node_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            
        }

        // 连接点按钮
        private void TieBtn_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)TieBtn.IsChecked)
            {
                EditFrame.Navigate(typeof(EditPage.InfoPage), "从左侧选择两个未连接点以连接它们，或者选择两个已连接的点以取消它们之间的连接。", new DrillInNavigationTransitionInfo());
            }
            else
            {
                // 让所有被选中的点恢复
                CreateOrUpdateSpringAnimation(1.0f);
                foreach (Border border in selectedBorderList)
                    border.StartAnimation(_springAnimation);
                selectedBorderList.Clear();

                EditFrame.Navigate(typeof(EditPage.InfoPage), null, new DrillInNavigationTransitionInfo());
            }
        }

        private void MenuBtn_Click(object sender, RoutedEventArgs e)
        {
            //await Snapshot.NewSnapshot(mindMapCanvas);

            this.Frame.Navigate(typeof(MenuPage), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft });
        }

        private void AddNodeBtn_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // 退出“连接点”的状态，让所有被选中的点恢复
            EditFrame.Navigate(typeof(EditPage.InfoPage), null, new DrillInNavigationTransitionInfo());
            TieBtn.IsChecked = false;
            CreateOrUpdateSpringAnimation(1.0f);
            foreach (Border border in selectedBorderList)
                border.StartAnimation(_springAnimation);
            selectedBorderList.Clear();

            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        // 添加点
        public void AddNodeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AddNodeTextBox.Text != "")
            {
                Node newNode = EventsManager.AddNode(AddNodeTextBox.Text);
                ConfigNodesBorder(new List<Node> { newNode });
                RefreshUnRedoBtn();
            }
        }

        // 撤销
        public void UndoBtn_Click(object sender, RoutedEventArgs e)
        {
            EventsManager.Undo();
            RefreshUnRedoBtn();
            ConfigNodesBorder();
            EditFrame.Navigate(typeof(EditPage.InfoPage), null, new DrillInNavigationTransitionInfo());
        }

        // 重做
        public void RedoBtn_Click(object sender, RoutedEventArgs e)
        {
            EventsManager.Redo();
            RefreshUnRedoBtn();
            ConfigNodesBorder();
            EditFrame.Navigate(typeof(EditPage.InfoPage), null, new DrillInNavigationTransitionInfo());
        }

        // 刷新撤销重做按钮
        public void RefreshUnRedoBtn()
        {
            UndoBtn.IsEnabled = EventsManager.CanUndo;
            RedoBtn.IsEnabled = EventsManager.CanRedo;
        }

        // MindMapBorder加载完成
        private void MindMapBorder_Loaded(object sender, RoutedEventArgs e)
        {
            mindMapCanvas = new MindMapCanvas();
            MindMapBorder.Child = mindMapCanvas;

            EventsManager.SetMindMapCanvas(mindMapCanvas);
            ConfigNodesBorder();

            MindMapScrollViewer.ChangeView(horizontalOffset: MindMapScrollViewer.ScrollableWidth / 2, verticalOffset: MindMapScrollViewer.ScrollableHeight / 2, zoomFactor: 1);
        }

        private void MindMapScrollViewer_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // 移动画布
            // 排除有点正在按下的情况
            if (nowPressedNode == null)
            { 
                // 排除在使用鼠标并且处于惯性的情况
                if (!(e.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse && e.IsInertial))
                    MindMapScrollViewer.ChangeView(
                        horizontalOffset: MindMapScrollViewer.HorizontalOffset - e.Delta.Translation.X,
                        verticalOffset: MindMapScrollViewer.VerticalOffset - e.Delta.Translation.Y,
                        zoomFactor: null);
            }
        }

        // MindMapScrollViewer大小改变
        private void MindMapScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 保持显示的相对位置不变
            MindMapScrollViewer.ChangeView(
                horizontalOffset: MindMapScrollViewer.HorizontalOffset - (e.NewSize.Width - e.PreviousSize.Width) / 2,
                verticalOffset: MindMapScrollViewer.VerticalOffset - (e.NewSize.Height - e.PreviousSize.Height) / 2,
                zoomFactor: null);
        }
    }
}
