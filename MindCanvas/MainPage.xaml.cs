using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.ApplicationModel.Resources;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;


// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace MindCanvas
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Node nowNode;// 正在操作的点
        private List<NodeControl> selectedBorderList = new List<NodeControl>();// 选中的点
        private Node nowPressedNode; // 正在按着的点
        private bool isMovingNode = false;// 是否有点正在被移动
        public static MindMapCanvas mindMapCanvas;
        public static MindMapInkCanvas mindMapInkCanvas;
        public static MainPage mainPage;

        // 资源加载器，用于翻译
        private ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

        public MainPage()
        {
            this.InitializeComponent();

            mainPage = this;

            // 这两个事件用来实现移动点
            ManipulationDelta += MainPage_ManipulationDelta;// 鼠标移动事件
            PointerReleased += MainPage_PointerReleased;// 鼠标释放事件

            // 擦除所有墨迹事件
            MindMapInkToolbar.EraseAllClicked += MindMapInkToolbar_EraseAllClicked;

            // 设置缓存
            NavigationCacheMode = NavigationCacheMode.Required;

            // 设置阴影
            SharedShadow.Receivers.Add(MindMapScrollViewer);
            EditBorder.Translation += new Vector3(0, 0, 32);
            InkToolbarBorder.Translation += new Vector3(0, 0, 32);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            RefreshUnRedoBtn();
            RefreshTheme();

            InkToolToggleSwitch.IsOn = false;
            MindMapInkToolbar.IsStencilButtonChecked = false;
        }

        // 擦除所有墨迹
        private void MindMapInkToolbar_EraseAllClicked(InkToolbar sender, object args)
        {
            EventsManager.ModifyInkCanvas(sender.TargetInkCanvas.InkPresenter.StrokeContainer);
            RefreshUnRedoBtn();
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
                MindMapBackgroundBorder.Background = new SolidColorBrush(Colors.White);
            }
            else if (ThemeHelper.ActualTheme == ElementTheme.Dark)
            {
                MenuBtnBorder.Background = new SolidColorBrush(Colors.Black);
                AppBarButtonsBorder.Background = new SolidColorBrush(Colors.Black);
                AppNameBorder.Background = new SolidColorBrush(Colors.Black);
                MindMapBackgroundBorder.Background = new SolidColorBrush(Colors.Black);
            }
        }

        // 配置点Border
        private void ConfigNodesBorder(List<Node> needConfig = null)
        {
            if (needConfig == null)
                needConfig = App.mindMap.nodes;

            foreach (Node node in needConfig)
            {
                NodeControl border = mindMapCanvas.ConvertNodeToBorder(node);
                border.PointerPressed += this.Node_Pressed;// 鼠标按下
                border.PointerReleased += this.Node_Released;// 鼠标释放
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
                path.PointerReleased += Path_PointerReleased;// 鼠标释放
            }
        }

        // 鼠标在线内释放，算按了一下线
        private void Path_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            Windows.UI.Xaml.Shapes.Path path = sender as Windows.UI.Xaml.Shapes.Path;
            Tie tie = mindMapCanvas.ConvertPathToTie(path);

            // 显示信息
            Dictionary<string, object> data = new Dictionary<string, object>
                {
                    { "tie", tie },
                    { "path", path },
                };
            ShowFrame(typeof(EditPage.EditTiePage), data);
        }

        // 鼠标在点内释放，算按了一下点
        private void Node_Released(object sender, PointerRoutedEventArgs e)
        {
            // 使EditFrame中的内容失去焦点
            EditFrame.IsEnabled = false;
            EditFrame.IsEnabled = true;

            NodeControl nowNodeBorder = sender as NodeControl;
            nowNode = mindMapCanvas.ConvertBorderToNode(nowNodeBorder);// 记为nowNode

            // 正在选择点
            if ((bool)TieBtn.IsChecked)
            {
                // 检查点是否已被选择
                if (selectedBorderList.Contains(nowNodeBorder))
                {
                    // 已被选择，现在又点了一次，所以取消选择
                    selectedBorderList.Remove(nowNodeBorder);
                    return;
                }

                // 此点未被选择，现在进行选择
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
                    {
                        Tie newTie = EventsManager.AddTie(node1, node2);
                        ConfigTiesPath(new List<Tie> { newTie });
                    }

                    RefreshUnRedoBtn();

                    // 不论如何，让这2个被选中的点恢复
                    foreach (NodeControl border in selectedBorderList)
                        border.IsSelected = false;
                    selectedBorderList.Clear();

                    // 恢复正常
                    TieBtn.IsChecked = false;
                    ShowFrame(typeof(EditPage.InfoPage));
                }
            }

            // 没在选择点，查看点信息
            else
            {
                // 只能同时选择一个点
                foreach (NodeControl border in selectedBorderList)
                    if (border != nowNodeBorder)
                        border.IsSelected = false;
                selectedBorderList = new List<NodeControl> { nowNodeBorder };

                // 显示信息
                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    { "node", nowNode },
                    { "border", nowNodeBorder },
                };
                ShowFrame(typeof(EditPage.EditNodePage), data);
            }
        }

        // 鼠标释放
        private void MainPage_PointerReleased(object sender, PointerRoutedEventArgs e)
        {

            // 如果有个点在鼠标释放时处于移动状态，就算进行了一次“移动点”
            // 之所以不写在Node_Released里面，是因为可能用户在移动点时鼠标移动太快，导致鼠标未在点内释放
            if (isMovingNode)
            {
                NodeControl border = mindMapCanvas.ConvertNodeToBorder(nowPressedNode);
                var left = Canvas.GetLeft(border);
                var top = Canvas.GetTop(border);

                EventsManager.ModifyNode(nowPressedNode,
                    left - mindMapCanvas.Width / 2 + border.ActualWidth / 2,
                    top - mindMapCanvas.Height / 2 + border.ActualHeight / 2);

                ConfigTiesPath(App.mindMap.GetTies(nowPressedNode));

                RefreshUnRedoBtn();
                isMovingNode = false;

                // 移动点完成，允许使用RepeatButton
                SetRepeatButtonIsHitTestVisible(true);
            }
            nowPressedNode = null;// 没有点被按下，之所以这句话不写在Node_Released里面
                                  // 是因为当鼠标释放时Node_Released会比MainPage_PointerReleased先运行
                                  // 那MainPage_PointerReleased就不能获取nowPressedNode了
        }

        // 鼠标按下border
        private void Node_Pressed(object sender, PointerRoutedEventArgs e)
        {
            NodeControl border = sender as NodeControl;
            nowPressedNode = mindMapCanvas.ConvertBorderToNode(border);
        }

        // 鼠标移动事件
        private void MainPage_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // 如果这时有个点正在被按下，就移动它
            // 之所以不使用检测“在点border内鼠标移动”的方式来处理这个
            // 是因为在拖动点时可能鼠标移动太快导致丢失点border
            if (nowPressedNode != null && !e.IsInertial)
            {
                NodeControl border = mindMapCanvas.ConvertNodeToBorder(nowPressedNode);
                var translationPoint = e.Delta.Translation;// 获取相对指针变化

                var newLeft = Canvas.GetLeft(border) + translationPoint.X * (1 / MindMapScrollViewer.ZoomFactor);
                var newTop = Canvas.GetTop(border) + translationPoint.Y * (1 / MindMapScrollViewer.ZoomFactor);

                Canvas.SetLeft(border, newLeft);
                Canvas.SetTop(border, newTop);

                var newX = newLeft + border.ActualWidth / 2;
                var newY = newTop + border.ActualHeight / 2;

                // 保持线连着点
                foreach (Tie tie in App.mindMap.GetTies(nowPressedNode))
                {
                    List<Node> nodes = App.mindMap.GetNodes(tie);
                    Node anotherNode;
                    if (nodes[0] != nowPressedNode)
                        anotherNode = nodes[0];
                    else
                        anotherNode = nodes[1];

                    string commands = string.Format("M {0} {1} C {4} {1} {4} {3} {2} {3}",
                        newX,
                        newY,
                        mindMapCanvas.Width / 2 + anotherNode.X,
                        mindMapCanvas.Height / 2 + anotherNode.Y,
                        (newX + mindMapCanvas.Width / 2 + anotherNode.X) / 2);
                    mindMapCanvas.ModifyTiePath(tie, commands);
                }

                isMovingNode = true;

                // 移动点期间，禁用RepeatButton
                SetRepeatButtonIsHitTestVisible(false);

                // 不用现在提交EventManager，因为鼠标还没释放（nowPressedNode != null）
            }
        }

        // 连接点按钮
        private void TieBtn_Click(object sender, RoutedEventArgs e)
        {
            InkToolToggleSwitch.IsOn = false;

            // 让所有被选中的点恢复
            foreach (NodeControl border in selectedBorderList)
                border.IsSelected = false;
            selectedBorderList.Clear();

            if (TieBtn.IsChecked.Value)
                // 从左侧选择两个未连接点以连接它们，或者选择两个已连接的点以取消它们之间的连接。
                ShowFrame(typeof(EditPage.InfoPage), resourceLoader.GetString("Code_SelectTwoUntiedNodes"));
            else
                ShowFrame(typeof(EditPage.InfoPage));
        }

        private void MenuBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MenuPage), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft });
        }

        private void AddNodeBtn_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ShowFrame(typeof(EditPage.InfoPage));

            // 退出“连接点”的状态，让所有被选中的点恢复
            TieBtn.IsChecked = false;
            foreach (NodeControl border in selectedBorderList)
                border.IsSelected = false;
            selectedBorderList.Clear();

            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        // 添加点
        private void AddNodeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AddNodeTextBox.Text != "")
            {
                Node newNode = EventsManager.AddNode(AddNodeTextBox.Text);
                ConfigNodesBorder(new List<Node> { newNode });
                RefreshUnRedoBtn();
                InkToolToggleSwitch.IsOn = false;

                // 如果新的点在可视区域外则移动可视区域到以新的点为中心
                if (!IsInViewport(newNode.X, newNode.Y))
                    ChangeView(newNode.X, newNode.Y, null);
            }
        }

        // 撤销
        private void UndoBtn_Click(object sender, RoutedEventArgs e)
        {
            EventsManager.Undo();
            RefreshUnRedoBtn();
            ConfigNodesBorder();
            ConfigTiesPath();

            ShowFrame(typeof(EditPage.InfoPage));
        }

        // 重做
        private void RedoBtn_Click(object sender, RoutedEventArgs e)
        {
            EventsManager.Redo();
            RefreshUnRedoBtn();
            ConfigNodesBorder();
            ConfigTiesPath();

            ShowFrame(typeof(EditPage.InfoPage));
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
            ConfigTiesPath();

            ChangeView(App.mindMap.visualCenterX, App.mindMap.visualCenterY, App.mindMap.zoomFactor);
        }

        // MindMapInkBorder加载完成
        private void MindMapInkBorder_Loaded(object sender, RoutedEventArgs e)
        {
            mindMapInkCanvas = new MindMapInkCanvas();
            MindMapInkBorder.Child = mindMapInkCanvas;
            MindMapInkToolbar.TargetInkCanvas = mindMapInkCanvas;

            EventsManager.SetMindMapInkCanvas(mindMapInkCanvas);
        }

        private void MindMapScrollViewer_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // 移动画布
            // 排除有点正在按下或者的情况
            if (nowPressedNode == null)
            {
                // 排除在使用鼠标并且处于惯性的情况
                if (!(e.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse && e.IsInertial))
                    MoveView(
                        relx: -e.Delta.Translation.X * (1 / MindMapScrollViewer.ZoomFactor) * 0.6,
                        rely: -e.Delta.Translation.Y * (1 / MindMapScrollViewer.ZoomFactor) * 0.6
                    );
            }
        }

        // MindMapScrollViewer大小改变
        private void MindMapScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 保持显示的相对位置不变
            MoveView();
        }

        // 触控书写支持
        private void TouchWritingBtn_Click(object sender, RoutedEventArgs e)
        {
            if (TouchWritingBtn.IsChecked == true)
            {
                mindMapInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;
            }
            else
            {
                mindMapInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen;
            }
        }

        // 墨迹书写模式切换
        private void InkToolToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (InkToolToggleSwitch.IsOn)
            {
                MindMapInkBorder.IsHitTestVisible = true;
                mindMapInkCanvas.InkPresenter.IsInputEnabled = true;
            }
            else
            {
                MindMapInkBorder.IsHitTestVisible = false;
                mindMapInkCanvas.InkPresenter.IsInputEnabled = false;
            }

            if (TouchWritingBtn.IsChecked == true)
                mindMapInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;
            else
                mindMapInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen;
        }

        // 尺子和量角器
        private void MindMapInkToolbar_IsStencilButtonCheckedChanged(InkToolbar sender, InkToolbarIsStencilButtonCheckedChangedEventArgs args)
        {
            InkPresenterRuler ruler = args.StencilButton.Ruler;// 直尺
            InkPresenterProtractor protractor = args.StencilButton.Protractor;// 量角器

            // Set Ruler Origin to Scrollviewer Viewport origin.
            // The purpose of this behavior is to allow the user to "grab" the
            // ruler and bring it into view no matter where the scrollviewer viewport
            // happens to be.  Note that this is accomplished by a simple translation
            // that adjusts to the zoom factor.  The additional ZoomFactor term is to
            // make ensure the scale of the InkPresenterRuler is invariant to Zoom.
            Matrix3x2 viewportTransform =
                Matrix3x2.CreateScale(MindMapScrollViewer.ZoomFactor) *
                Matrix3x2.CreateTranslation(
                   (float)MindMapScrollViewer.HorizontalOffset,
                   (float)MindMapScrollViewer.VerticalOffset) *
                Matrix3x2.CreateScale(1.0f / MindMapScrollViewer.ZoomFactor);

            ruler.Transform = viewportTransform;
            protractor.Transform = viewportTransform;
        }

        // EditFrame跳转
        public void ShowFrame(Type sourcePageType, object parameter = null)
        {
            Dictionary<string, object> data = parameter as Dictionary<string, object>;
            if (data != null)
            {
                EditFrame.Navigate(sourcePageType, data, new DrillInNavigationTransitionInfo());
            }
            else
                EditFrame.Navigate(sourcePageType, parameter, new DrillInNavigationTransitionInfo());
        }

        // 默认值设置
        private void DefaultSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(DefaultSettingsPage));
        }

        // 移动画布按钮
        private void Chevron_Click(object sender, RoutedEventArgs e)
        {
            string tag = (string)(sender as RepeatButton).Tag;

            switch (tag)
            {
                case "Up": MoveView(rely: -20); break;
                case "Down": MoveView(rely: 20); break;
                case "Left": MoveView(relx: -20); break;
                case "Right": MoveView(relx: 20); break;
            }
        }

        // 放大、缩小
        private void Zoom_Click(object sender, RoutedEventArgs e)
        {
            string tag = (string)(sender as Button).Tag;

            switch (tag)
            {
                case "ZoomOut": MoveView(power: 1 / 1.2f); break;// 缩小
                case "ZoomIn": MoveView(power: 1.2f); break;// 放大
                case "Fit":// 查看全览
                    if (App.mindMap.nodes.Count() == 0)
                        ChangeView(0, 0, InitialValues.MinZoomFactor);

                    else
                        ChangeView(App.mindMap.nodes[0].X, App.mindMap.nodes[0].Y, InitialValues.MinZoomFactor);

                    break;
            }
        }

        private void MindMapScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (e.IsIntermediate)
            {
                ZoomInBtn.IsEnabled = MindMapScrollViewer.ZoomFactor * 1.2 <= InitialValues.MaxZoomFactor;
                ZoomOutBtn.IsEnabled = MindMapScrollViewer.ZoomFactor / 1.2 >= InitialValues.MinZoomFactor;
            }
        }

        // 禁用鼠标滚轮
        private void MindMapGrid_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            float power = e.GetCurrentPoint(MindMapScrollViewer).Properties.MouseWheelDelta > 0 ? 1.1f : 1 / 1.1f;
            double left = e.GetCurrentPoint(MindMapScrollViewer).Position.X;
            double top = e.GetCurrentPoint(MindMapScrollViewer).Position.Y;
            double relx, rely;

            if (MindMapScrollViewer.ZoomFactor * power > InitialValues.MinZoomFactor && MindMapScrollViewer.ZoomFactor * power < InitialValues.MaxZoomFactor)
            {
                relx = (left - MindMapScrollViewer.ActualWidth / 2) * power * 0.1;
                rely = (top - MindMapScrollViewer.ActualHeight / 2) * power * 0.1;

                MoveView(relx, rely, power);
            }

            e.Handled = true;
        }

        private void EditFrame_Loaded(object sender, RoutedEventArgs e)
        {
            ShowFrame(typeof(EditPage.InfoPage));
        }

        // 改变可视区域（接受相对于画布中间的x和y）
        private void ChangeView(double? x, double? y, float? zoomFactor)
        {
            double horizontalOffset, verticalOffset;

            if (!MindMapBorder.IsLoaded)
                return;

            if (zoomFactor == null)
                zoomFactor = MindMapScrollViewer.ZoomFactor;

            if (x == null)
                x = App.mindMap.visualCenterX;

            if (y == null)
                y = App.mindMap.visualCenterY;

            horizontalOffset = mindMapCanvas.Width * zoomFactor.Value / 2 + x.Value * zoomFactor.Value - MindMapScrollViewer.ActualWidth / 2;
            verticalOffset = mindMapCanvas.Height * zoomFactor.Value / 2 + y.Value * zoomFactor.Value - MindMapScrollViewer.ActualHeight / 2;

            MindMapScrollViewer.ChangeView(horizontalOffset, verticalOffset, zoomFactor);

            EventsManager.ModifyViewport(x, y, zoomFactor);
        }

        // 移动可视区域（接受相对于画布中间的x和y的相对移动值和相对缩放倍数）
        private void MoveView(double relx = 0, double rely = 0, float power = 1)
        {
            double horizontalOffset, verticalOffset, x, y;
            float zoomFactor;

            if (!MindMapBorder.IsLoaded)
                return;

            x = relx + App.mindMap.visualCenterX;
            y = rely + App.mindMap.visualCenterY;

            zoomFactor = MindMapScrollViewer.ZoomFactor * power;
            horizontalOffset = mindMapCanvas.Width * zoomFactor / 2 + x * zoomFactor - MindMapScrollViewer.ActualWidth / 2;
            verticalOffset = mindMapCanvas.Height * zoomFactor / 2 + y * zoomFactor - MindMapScrollViewer.ActualHeight / 2;
            MindMapScrollViewer.ChangeView(horizontalOffset, verticalOffset, zoomFactor);

            EventsManager.ModifyViewport(x, y, zoomFactor);
        }

        // 设置RepeatButton的IsHitTestVisible
        private void SetRepeatButtonIsHitTestVisible(bool value)
        {
            UpRepeatButton.IsHitTestVisible = value;
            DownRepeatButton.IsHitTestVisible = value;
            LeftRepeatButton.IsHitTestVisible = value;
            RightRepeatButton.IsHitTestVisible = value;
        }

        // 判断某点是否在可视区域
        private bool IsInViewport(double x, double y)
        {
            return App.mindMap.visualCenterX - MindMapScrollViewer.ActualWidth / 2 < x 
                && x < App.mindMap.visualCenterX + MindMapScrollViewer.ActualWidth / 2
                && App.mindMap.visualCenterY - MindMapScrollViewer.ActualHeight / 2 < y
                && y <  App.mindMap.visualCenterY + MindMapScrollViewer.ActualHeight / 2;

        }
    }
}
