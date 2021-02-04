﻿using System;
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
using Microsoft.Toolkit.Uwp.Helpers;
using Windows.Storage;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Core;
using Windows.UI.Input.Inking.Core;
using Windows.UI.Input.Inking;


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

            RefreshTheme();
            RefreshUnRedoBtn();

            ShowFrame(typeof(EditPage.InfoPage));            
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
                selectedBorderList = new List<NodeControl>{nowNodeBorder};

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

                var newLeft = Canvas.GetLeft(border) + translationPoint.X * (1/MindMapScrollViewer.ZoomFactor);
                var newTop = Canvas.GetTop(border) + translationPoint.Y * (1/MindMapScrollViewer.ZoomFactor);

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

                // 不用现在提交EventManager，因为鼠标还没释放（nowPressedNode != null）
            }
        }

        // 连接点按钮
        private void TieBtn_Click(object sender, RoutedEventArgs e)
        {
            // 让所有被选中的点恢复
            foreach (NodeControl border in selectedBorderList)
                border.IsSelected = false;
            selectedBorderList.Clear();

            if ((bool)TieBtn.IsChecked)
                ShowFrame(typeof(EditPage.InfoPage), "从左侧选择两个未连接点以连接它们，或者选择两个已连接的点以取消它们之间的连接。");
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

            MindMapScrollViewer.ChangeView(horizontalOffset: MindMapScrollViewer.ScrollableWidth / 2, verticalOffset: MindMapScrollViewer.ScrollableHeight / 2, zoomFactor: 1);
        }

        // MindMapInkBorder加载完成
        private void MindMapInkBorder_Loaded(object sender, RoutedEventArgs e)
        {
            InkToolToggleSwitch.IsOn = false;
            MindMapInkToolbar.IsStencilButtonChecked = false;

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
                Canvas.SetZIndex(MindMapInkBorder, 1);
                Canvas.SetZIndex(MindMapBorder, 0);
                mindMapInkCanvas.InkPresenter.IsInputEnabled = true;
            }
            else
            {
                Canvas.SetZIndex(MindMapInkBorder, 0);
                Canvas.SetZIndex(MindMapBorder, 1);
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
        public void ShowFrame(Type sourcePageType, object parameter=null)
        {
            Dictionary<string, object> data = parameter as Dictionary<string, object>;
            if (data != null)
            {
                EditFrame.Navigate(sourcePageType, data, new DrillInNavigationTransitionInfo());
            }
            else
                EditFrame.Navigate(sourcePageType, parameter, new DrillInNavigationTransitionInfo());

        }

        // 禁用鼠标滚轮
        /*private void MindMapGrid_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            MindMapScrollViewer.ChangeView(
                horizontalOffset: MindMapScrollViewer.HorizontalOffset,
                verticalOffset: MindMapScrollViewer.HorizontalOffset,
                zoomFactor: MindMapScrollViewer.ZoomFactor * e.GetCurrentPoint(MindMapScrollViewer).Properties.MouseWheelDelta/10000.0f);
            e.Handled = true;
        }*/
    }
}
