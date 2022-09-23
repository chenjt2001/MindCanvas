using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Formatters.Binary;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Storage.Streams;
using Windows.System;
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
        private Tie nowTie;// 正在操作的线
        private Node nowPressedNode; // 正在按着的点
        private bool isMovingNode = false;// 是否有点正在被移动
        private object clipboardContentCache;// 剪贴板内容缓存
        private Windows.Foundation.Point RightTappedPosition;// 右键的位置（MindMapCanvas标准坐标）

        public static MindMapCanvas mindMapCanvas;
        public static MindMapInkCanvas mindMapInkCanvas;
        public static MainPage mainPage;

        // 资源加载器，用于翻译
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

        public MainPage()
        {
            LogHelper.Debug("MainPage");

            this.InitializeComponent();

            mainPage = this;

            // 这两个事件用来实现移动点
            ManipulationDelta += MainPage_ManipulationDelta;// 鼠标移动事件
            PointerReleased += MainPage_PointerReleased;// 鼠标释放事件

            // 擦除所有墨迹事件
            MindMapInkToolbar.EraseAllClicked += MindMapInkToolbar_EraseAllClicked;

            // 设置缓存
            NavigationCacheMode = NavigationCacheMode.Required;

            // 应用设置
            Settings.Apply();

            // 默认显示侧栏
            ShowTheSidebarMenu = true;

            // 请求评分
            if (Settings.TotalLaunchCount == 5)
                Toast.RequestRatingsAndReviews();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            RefreshUnRedoBtn();
            RefreshTheme();
            EventsManager.RefreshAppTitle();

            InkToolToggleSwitch.IsOn = false;
            MindMapInkToolbar.IsStencilButtonChecked = false;
        }

        /// <summary>擦除所有墨迹</summary>
        private void MindMapInkToolbar_EraseAllClicked(InkToolbar sender, object args)
        {
            EventsManager.ModifyInkCanvas(sender.TargetInkCanvas.InkPresenter.StrokeContainer);
            RefreshUnRedoBtn();
        }

        /// <summary>清除被选中的点</summary>
        private void SelectedNodeControlListClear()
        {
            foreach (NodeControl nodeControl in mindMapCanvas.SelectedNodeControlList.ToList())
                nodeControl.State &= ~NodeControlState.Selected;//border.IsSelected = false;
        }

        /// <summary>刷新主题设置</summary>
        public void RefreshTheme()
        {
            // 应用主题颜色
            if (ThemeHelper.ActualTheme == ElementTheme.Light)
            {
                //MenuBtnBorder.Background = new SolidColorBrush(Colors.White);
                //AppBarButtonsBorder.Background = new SolidColorBrush(Colors.White);
                //AppNameBorder.Background = new SolidColorBrush(Colors.White);
                MindMapBackgroundBorder.Background = new SolidColorBrush(Colors.White);
            }
            else if (ThemeHelper.ActualTheme == ElementTheme.Dark)
            {
                //MenuBtnBorder.Background = new SolidColorBrush(Colors.Black);
                //AppBarButtonsBorder.Background = new SolidColorBrush(Colors.Black);
                //AppNameBorder.Background = new SolidColorBrush(Colors.Black);
                MindMapBackgroundBorder.Background = new SolidColorBrush(Colors.DimGray);
            }
        }

        /// <summary>配置点Border</summary>
        private void ConfigNodesBorder(List<Node> needConfig = null)
        {
            if (needConfig == null)
                needConfig = App.mindMap.Nodes;

            foreach (Node node in needConfig)
            {
                NodeControl nodeControl = mindMapCanvas.ConvertNodeToBorder(node);
                nodeControl.PointerEntered += this.NodeControl_PointerEntered;// 鼠标进入
                nodeControl.PointerPressed += this.NodeControl_Pressed;// 鼠标按下
                nodeControl.PointerReleased += this.NodeControl_Released;// 鼠标释放
                nodeControl.RightTapped += this.NodeControl_RightTapped;// 右键
                nodeControl.PointerExited += this.NodeControl_PointerExited;// 鼠标退出
            }
        }

        /// <summary>鼠标退出点</summary>
        private void NodeControl_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            NodeControl nodeControl = sender as NodeControl;
            nodeControl.State &= ~NodeControlState.Highlighted;
        }

        /// <summary>鼠标进入点</summary>
        private void NodeControl_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            NodeControl nodeControl = sender as NodeControl;

            nodeControl.DescriptionToolTip.IsEnabled = true;
            nodeControl.State |= NodeControlState.Highlighted;
        }

        /// <summary>右键单击点</summary>
        private void NodeControl_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            // 创建右键菜单
            ContextMenuFlyout contextMenuFlyout = new ContextMenuFlyout();

            MenuFlyoutItem item1 = contextMenuFlyout.AddItem(resourceLoader.GetString("Code_Copy"),// 复制
                                                             VirtualKey.C,
                                                             VirtualKeyModifiers.None,
                                                             "\xE8C8");

            MenuFlyoutItem item2 = contextMenuFlyout.AddItem(resourceLoader.GetString("Code_Cut"),// 剪切
                                                             VirtualKey.X,
                                                             VirtualKeyModifiers.None,
                                                             "\xE8C6");

            MenuFlyoutItem item3 = contextMenuFlyout.AddItem(resourceLoader.GetString("Code_Delete"),// 删除
                                                            VirtualKey.D,
                                                            VirtualKeyModifiers.None,
                                                            "\xE74D");

            item1.Click += CopyNodeMenuFlyoutItem_Click;
            item2.Click += CutNodeMenuFlyoutItem_Click;
            item3.Click += DeleteNodeMenuFlyoutItem_Click;

            contextMenuFlyout.ShowAt(this, e.GetPosition(this));
        }

        /// <summary>点击了右键的删除点按钮</summary>
        private void DeleteNodeMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            NodeControl nodeControl = mindMapCanvas.ConvertNodeToBorder(nowNode);

            // 如果这个点在selectedBorderList里，则移除，避免在连接点时发生异常
            if (nodeControl.State.HasFlag(NodeControlState.Selected))
                nodeControl.State &= ~NodeControlState.Selected;

            EventsManager.RemoveNode(nowNode);

            RefreshUnRedoBtn();
            ShowFrame(typeof(EditPage.InfoPage));
        }

        /// <summary>点击了右键的复制点按钮</summary>
        private void CopyNodeMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            EventsManager.CopyNode(nowNode);
        }

        /// <summary>点击了右键的剪切点按钮</summary>
        private void CutNodeMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            NodeControl nodeControl = mindMapCanvas.ConvertNodeToBorder(nowNode);

            // 如果这个点在selectedBorderList里，则移除，避免在连接点时发生异常
            if (nodeControl.State.HasFlag(NodeControlState.Selected))
                nodeControl.State &= ~NodeControlState.Selected;

            EventsManager.CutNode(nowNode);

            RefreshUnRedoBtn();
            ShowFrame(typeof(EditPage.InfoPage));
        }

        /// <summary>配置线Path</summary>
        public void ConfigTiesPath(List<Tie> needConfig = null)
        {
            if (needConfig == null)
                needConfig = App.mindMap.Ties;

            foreach (Tie tie in needConfig)
            {
                Windows.UI.Xaml.Shapes.Path path = mindMapCanvas.ConvertTieToPath(tie);
                path.PointerReleased += Path_PointerReleased;// 鼠标释放
                path.RightTapped += Path_RightTapped;
            }
        }

        /// <summary>右键单击线</summary>
        private void Path_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            // 创建右键菜单
            ContextMenuFlyout contextMenuFlyout = new ContextMenuFlyout();
            MenuFlyoutItem item = contextMenuFlyout.AddItem(resourceLoader.GetString("Code_Delete"),// 删除
                                                            VirtualKey.D,
                                                            VirtualKeyModifiers.None,
                                                            "\xE74D");
            item.Click += DeleteTieMenuFlyoutItem_Click;

            contextMenuFlyout.ShowAt(this, e.GetPosition(this));
        }

        /// <summary>点击了右键的删除线按钮</summary>
        private void DeleteTieMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            EventsManager.RemoveTie(nowTie);
            RefreshUnRedoBtn();
            ShowFrame(typeof(EditPage.InfoPage));
        }

        /// <summary>鼠标在线内释放，算按了一下线</summary>
        private void Path_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            Windows.UI.Xaml.Shapes.Path path = sender as Windows.UI.Xaml.Shapes.Path;
            nowTie = mindMapCanvas.ConvertPathToTie(path);

            // 显示信息
            ShowFrame(typeof(EditPage.EditTiePage), nowTie);
        }

        /// <summary>鼠标在点内释放，算按了一下点</summary>
        private void NodeControl_Released(object sender, PointerRoutedEventArgs e)
        {
            // 使EditFrame中的内容失去焦点
            EditFrame.IsEnabled = false;
            EditFrame.IsEnabled = true;

            NodeControl nowNodeControl = sender as NodeControl;
            nowNode = mindMapCanvas.ConvertBorderToNode(nowNodeControl);// 记为nowNode

            nowNodeControl.State &= ~NodeControlState.Pressed;

            // 正在选择点
            if ((bool)TieBtn.IsChecked)
            {
                // 检查点是否已被选择
                if (nowNodeControl.State.HasFlag(NodeControlState.Selected))
                {
                    // 已被选择，现在又点了一次，所以取消选择
                    nowNodeControl.State &= ~NodeControlState.Selected;
                    return;
                }

                // 此点未被选择，现在进行选择
                nowNodeControl.State |= NodeControlState.Selected;// 把当前选择的点计入selectedBorderList

                // 如果已经选好了两个点，就连接
                if (mindMapCanvas.SelectedNodeControlList.Count() == 2)
                {
                    Node node1, node2;
                    node1 = mindMapCanvas.ConvertBorderToNode(mindMapCanvas.SelectedNodeControlList.ElementAt(0));
                    node2 = mindMapCanvas.ConvertBorderToNode(mindMapCanvas.SelectedNodeControlList.ElementAt(1));

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
                    SelectedNodeControlListClear();

                    // 恢复正常
                    TieBtn.IsChecked = false;
                    ShowFrame(typeof(EditPage.InfoPage));
                }
            }

            // 没在选择点，查看点信息
            else
            {
                nowNodeControl.State |= NodeControlState.Selected;

                // 只能同时选择一个点
                foreach (NodeControl nodeControl in mindMapCanvas.SelectedNodeControlList.ToList())
                    if (nodeControl != nowNodeControl)
                        nodeControl.State &= ~NodeControlState.Selected;//border.IsSelected = false;

                // 显示信息
                ShowFrame(typeof(EditPage.EditNodePage), nowNode);
            }
        }

        /// <summary>鼠标释放</summary>
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
            else
            {

            }

            nowPressedNode = null;// 没有点被按下，之所以这句话不写在Node_Released里面
                                  // 是因为当鼠标释放时Node_Released会比MainPage_PointerReleased先运行
                                  // 那MainPage_PointerReleased就不能获取nowPressedNode了
        }

        /// <summary>鼠标按下border</summary>
        private void NodeControl_Pressed(object sender, PointerRoutedEventArgs e)
        {
            NodeControl nodeControl = sender as NodeControl;

            nowPressedNode = mindMapCanvas.ConvertBorderToNode(nodeControl);

            nodeControl.DescriptionToolTip.IsEnabled = false;
            nodeControl.State |= NodeControlState.Pressed;
        }

        /// <summary>鼠标移动事件</summary>
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

                // 保持线连着点
                foreach (Tie tie in App.mindMap.GetTies(nowPressedNode))
                {
                    List<Node> nodes = App.mindMap.GetNodes(tie);
                    Node anotherNode;
                    if (nodes[0] != nowPressedNode)
                        anotherNode = nodes[0];
                    else
                        anotherNode = nodes[1];

                    NodeControl.ModifyPathInCanvas(mindMapCanvas.ConvertTieToPath(tie),
                                                   border,
                                                   mindMapCanvas.ConvertNodeToBorder(anotherNode));
                }

                isMovingNode = true;

                // 移动点期间，禁用RepeatButton
                SetRepeatButtonIsHitTestVisible(false);

                // 不用现在提交EventManager，因为鼠标还没释放（nowPressedNode != null）
            }
        }

        /// <summary>连接点按钮</summary>
        private void TieBtn_Click(object sender, RoutedEventArgs e)
        {
            InkToolToggleSwitch.IsOn = false;

            // 让所有被选中的点恢复
            SelectedNodeControlListClear();

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
            SelectedNodeControlListClear();

            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        /// <summary>添加点</summary>
        private void ConfirmAddingNodeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AddNodeTextBox.Text != "")
            {
                Node newNode = EventsManager.AddNode(AddNodeTextBox.Text);
                ConfigNodesBorder(new List<Node> { newNode });
                RefreshUnRedoBtn();
                InkToolToggleSwitch.IsOn = false;
                AddNodeTextBox.Text = "";

                // 如果新的点在可视区域外则移动可视区域到以新的点为中心
                if (!IsInViewport(newNode.X, newNode.Y))
                    ChangeView(newNode.X, newNode.Y, null);
            }
        }

        /// <summary>撤销重做</summary>
        private void UnRedoBtn_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as Button).Tag)
            {
                case "Undo":// 撤销
                    EventsManager.Undo();
                    break;
                case "Redo":// 重做
                    EventsManager.Redo();
                    break;
            }

            RefreshUnRedoBtn();
            ConfigNodesBorder();
            ConfigTiesPath();
            //MoveView();

            ShowFrame(typeof(EditPage.InfoPage));
        }

        /// <summary>刷新撤销重做按钮</summary>
        public void RefreshUnRedoBtn()
        {
            UndoBtn.IsEnabled = EventsManager.CanUndo;
            RedoBtn.IsEnabled = EventsManager.CanRedo;
        }

        /// <summary>MindMapBorder加载完成</summary>
        private void MindMapBorder_Loaded(object sender, RoutedEventArgs e)
        {
            mindMapCanvas = new MindMapCanvas();
            MindMapBorder.Child = mindMapCanvas;

            EventsManager.SetMindMapCanvas(mindMapCanvas);
            ConfigNodesBorder();
            ConfigTiesPath();

            ChangeView(App.mindMap.VisualCenterX, App.mindMap.VisualCenterY, App.mindMap.ZoomFactor);
        }

        /// <summary>MindMapInkBorder加载完成</summary>
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

        /// <summary>MindMapScrollViewer大小改变</summary>
        private void MindMapScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 保持显示的相对位置不变
            MoveView();
        }

        /// <summary>触控书写支持</summary>
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

        /// <summary>墨迹书写模式切换</summary>
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

        /// <summary>尺子和量角器</summary>
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

        /// <summary>EditFrame跳转</summary>
        public void ShowFrame(Type sourcePageType, object parameter = null)
        {
            NavigationTransitionInfo info;
            if (sourcePageType.IsInstanceOfType(EditFrame.Content))
                info = new SuppressNavigationTransitionInfo();
            else
                info = new DrillInNavigationTransitionInfo();

            EditFrame.Navigate(sourcePageType, parameter, info);
            EditFrame.BackStack.Clear();
        }

        /// <summary>默认值设置</summary>
        private void DefaultSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(DefaultSettingsPage));
        }

        /// <summary>移动画布按钮</summary>
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

        /// <summary>放大、缩小</summary>
        private void Zoom_Click(object sender, RoutedEventArgs e)
        {
            string tag = (string)(sender as Button).Tag;

            switch (tag)
            {
                case "ZoomOut": MoveView(power: 1 / 1.2f); break;// 缩小
                case "ZoomIn": MoveView(power: 1.2f); break;// 放大
                case "Fit":// 查看全览
                    if (App.mindMap.Nodes.Count() == 0)
                        ChangeView(0, 0, InitialValues.MinZoomFactor);

                    else
                        ChangeView(App.mindMap.Nodes[0].X, App.mindMap.Nodes[0].Y, InitialValues.MinZoomFactor);
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

            double x = (MindMapScrollViewer.HorizontalOffset + MindMapScrollViewer.ActualWidth / 2 - mindMapCanvas.Width * MindMapScrollViewer.ZoomFactor / 2) / MindMapScrollViewer.ZoomFactor;
            double y = (MindMapScrollViewer.VerticalOffset + MindMapScrollViewer.ActualHeight / 2 - mindMapCanvas.Height * MindMapScrollViewer.ZoomFactor / 2) / MindMapScrollViewer.ZoomFactor;
            float zoomFactor = MindMapScrollViewer.ZoomFactor;
            EventsManager.ModifyViewport(x, y, zoomFactor);
        }

        /// <summary>禁用鼠标滚轮</summary>
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

        /// <summary>改变可视区域</summary>
        /// <param name="x">相对于画布中间的x</param>
        /// <param name="y">相对于画布中间的y</param>
        /// <param name="zoomFactor">缩放倍数</param>
        public void ChangeView(double? x, double? y, float? zoomFactor)
        {
            double horizontalOffset, verticalOffset;

            if (!MindMapBorder.IsLoaded)
                return;

            float _zoomFactor = zoomFactor ?? App.mindMap.ZoomFactor;
            FixZoomFactor(ref _zoomFactor);
            zoomFactor = _zoomFactor;

            if (!x.HasValue)
                x = App.mindMap.VisualCenterX;

            if (!y.HasValue)
                y = App.mindMap.VisualCenterY;

            horizontalOffset = mindMapCanvas.Width * zoomFactor.Value / 2 + x.Value * zoomFactor.Value - MindMapScrollViewer.ActualWidth / 2;
            verticalOffset = mindMapCanvas.Height * zoomFactor.Value / 2 + y.Value * zoomFactor.Value - MindMapScrollViewer.ActualHeight / 2;

            MindMapScrollViewer.ChangeView(horizontalOffset, verticalOffset, zoomFactor.Value);

            EventsManager.ModifyViewport(x, y, zoomFactor);
        }

        /// <summary>移动可视区域</summary>
        /// <param name="relx">相对移动x</param>
        /// <param name="rely">相对移动y</param>
        /// <param name="power">相对缩放倍数</param>
        private void MoveView(double relx = 0, double rely = 0, float power = 1)
        {
            double horizontalOffset, verticalOffset, x, y;
            float zoomFactor;

            if (!MindMapBorder.IsLoaded)
                return;

            x = relx + App.mindMap.VisualCenterX;
            y = rely + App.mindMap.VisualCenterY;
            zoomFactor = App.mindMap.ZoomFactor * power;

            FixZoomFactor(ref zoomFactor);

            horizontalOffset = mindMapCanvas.Width * zoomFactor / 2 + x * zoomFactor - MindMapScrollViewer.ActualWidth / 2;
            verticalOffset = mindMapCanvas.Height * zoomFactor / 2 + y * zoomFactor - MindMapScrollViewer.ActualHeight / 2;
            MindMapScrollViewer.ChangeView(horizontalOffset, verticalOffset, zoomFactor);

            Debug.WriteLine(zoomFactor);
            Debug.WriteLine(MindMapScrollViewer.HorizontalOffset.ToString() + " " + MindMapScrollViewer.VerticalOffset.ToString());

            // 之所以在这里写ModifyViewport而不是交给MindMapScrollViewer_ViewChanged处理，是因为
            // MindMapScrollViewer_ManipulationDelta函数可能被调用多次后才发生一次
            // MindMapScrollViewer_ViewChanged，从而导致App.mindMap.VisualCenterX等没有实时更新
            EventsManager.ModifyViewport(x, y, zoomFactor);
        }

        /// <summary>确保ZoomFactor规范化</summary>
        private void FixZoomFactor(ref float zoomFactor)
        {
            if (zoomFactor < InitialValues.MinZoomFactor)
                zoomFactor = InitialValues.MinZoomFactor;
            else if (zoomFactor > InitialValues.MaxZoomFactor)
                zoomFactor = InitialValues.MaxZoomFactor;
        }

        /// <summary>设置RepeatButton的IsHitTestVisible</summary>
        private void SetRepeatButtonIsHitTestVisible(bool value)
        {
            UpRepeatButton.IsHitTestVisible = value;
            DownRepeatButton.IsHitTestVisible = value;
            LeftRepeatButton.IsHitTestVisible = value;
            RightRepeatButton.IsHitTestVisible = value;
        }

        /// <summary>判断某点是否在可视区域</summary>
        private bool IsInViewport(double x, double y)
        {
            return App.mindMap.VisualCenterX - MindMapScrollViewer.ActualWidth / MindMapScrollViewer.ZoomFactor / 2 < x
                && x < App.mindMap.VisualCenterX + MindMapScrollViewer.ActualWidth / MindMapScrollViewer.ZoomFactor / 2
                && App.mindMap.VisualCenterY - MindMapScrollViewer.ActualHeight / MindMapScrollViewer.ZoomFactor / 2 < y
                && y < App.mindMap.VisualCenterY + MindMapScrollViewer.ActualHeight / MindMapScrollViewer.ZoomFactor / 2;
        }

        /// <summary>按下Enter键就输入完成</summary>
        private void AddNodeTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && AddNodeTextBox.Text != "")
            {
                ConfirmAddingNodeBtn_Click(sender, e);
            }
        }

        /// <summary>右键或触摸设备长按</summary>
        private async void MindMapBackgroundBorder_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            // 检查是否可粘贴
            DataPackageView dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains("MindCanvas Node"))
            {
                // 从流中读取序列化后的内容并恢复
                var streamRef = await dataPackageView.GetDataAsync("MindCanvas Node") as RandomAccessStreamReference;
                using (IRandomAccessStreamWithContentType stream = await streamRef.OpenReadAsync())
                {
                    var buffer = new Windows.Storage.Streams.Buffer((uint)stream.Size);
                    await stream.ReadAsync(buffer, (uint)stream.Size, InputStreamOptions.None);

                    BinaryFormatter formatter = new BinaryFormatter();
                    object data = formatter.Deserialize(buffer.AsStream());
                    clipboardContentCache = data;
                }
                PasteMenuFlyoutItem.IsEnabled = true;
            }
            else
            {
                PasteMenuFlyoutItem.IsEnabled = false;
            }

            RightTappedPosition = e.GetPosition(mindMapCanvas);
            RightTappedPosition.X -= mindMapCanvas.Width / 2;
            RightTappedPosition.Y -= mindMapCanvas.Height / 2;

            // 显示右键菜单
            ContextMenuFlyout.ShowAt(this, e.GetPosition(this));
        }

        /// <summary>删除所有点</summary>
        private void DeleteAllNodesMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            SelectedNodeControlListClear();

            EventsManager.RemoveAllNodes();

            RefreshUnRedoBtn();
            ShowFrame(typeof(EditPage.InfoPage));
        }

        /// <summary>删除所有线</summary>
        private void DeleteAllTiesMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            EventsManager.RemoveAllTies();
            RefreshUnRedoBtn();
            ShowFrame(typeof(EditPage.InfoPage));
        }

        /// <summary>用户输入文本</summary>
        private void SearchAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {

            // Only get results when it was a user typing,
            // otherwise assume the value got filled in by TextMemberPath
            // or the handler for SuggestionChosen.
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                List<Item> result = EventsManager.SearchMindMap(sender.Text);
                if (result.Count > 0)
                    sender.ItemsSource = result;
                else
                    sender.ItemsSource = new List<Item> { new Item(resourceLoader.GetString("Code_NoResultsFound")) };// No results found
            }
        }

        /// <summary>用户在建议列表中选择某个建议</summary>
        private void SearchAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            // Set sender.Text. You can use args.SelectedItem to build your text string.
            Item selectedItem = args.SelectedItem as Item;
            if (selectedItem.Text != resourceLoader.GetString("Code_NoResultsFound"))
            {
                string[] words = selectedItem.Text.Split(new char[] { ' ', ':' });

                switch (words[0])
                {
                    case "Node":
                        Node node = App.mindMap.GetNode((int)selectedItem.Tag);
                        ChangeView(node.X, node.Y, null);
                        ShowFrame(typeof(EditPage.EditNodePage), node);
                        break;
                    case "Tie":
                        Tie tie = App.mindMap.GetTie((int)selectedItem.Tag);
                        List<Node> nodes = App.mindMap.GetNodes(tie);
                        ChangeView((nodes[0].X + nodes[1].X) / 2, (nodes[0].Y + nodes[1].Y) / 2, null);
                        ShowFrame(typeof(EditPage.EditTiePage), tie);
                        break;
                }
            }
        }

        /// <summary>用户提交某个查询</summary>
        private void SearchAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion == null)
            {
                // Use args.QueryText to determine what to do.
                ShowFrame(typeof(EditPage.SearchResultPage), args.QueryText);
            }
        }

        /// <summary>右键隐藏或显示侧栏</summary>
        private void ShowTheSidebarMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            ShowTheSidebarMenu = !ShowTheSidebarMenu;
        }

        private bool showTheSidebarMenu;
        private bool ShowTheSidebarMenu
        {
            get => showTheSidebarMenu;
            set
            {
                showTheSidebarMenu = value;

                if (value)
                {
                    // 显示
                    RightBottomGrid.Visibility = Visibility.Visible;
                    ScrollViewerWarpperGrid.CornerRadius = new CornerRadius(0, 10, 0, 0);
                    Grid.SetColumnSpan(LeftBottomGrid, 1);
                    Effects.SetShadow(InkToolbarBorder, CommonShadow);
                    Effects.SetShadow(SearchBorder, CommonShadow);
                    Effects.SetShadow(EditBorder, CommonShadow);

                    // 不加这个的话阴影不能即时出来
                    foreach (AttachedShadowElementContext context in CommonShadow.EnumerateElementContexts())
                    {
                        context.ClearAndDisposeResources();
                        context.CreateResources();
                    }
                }
                else
                {
                    // 隐藏
                    RightBottomGrid.Visibility = Visibility.Collapsed;
                    ScrollViewerWarpperGrid.CornerRadius = new CornerRadius(0);
                    Grid.SetColumnSpan(LeftBottomGrid, 2);

                    Effects.SetShadow(SearchBorder, null);
                    Effects.SetShadow(EditBorder, null);
                }
            }
        }

        /// <summary>粘贴</summary>
        private void PasteMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (clipboardContentCache is Node clipboardNode)
            {
                EventsManager.PasteNode(clipboardNode, RightTappedPosition);
                ConfigNodesBorder(new List<Node> { clipboardNode });
                RefreshUnRedoBtn();
            }
        }

        /// <summary>两只手指在触摸板捏合时会发生该事件</summary>
        //private void MindMapScrollViewer_DirectManipulationCompleted(object sender, object e) { }
    }
}
