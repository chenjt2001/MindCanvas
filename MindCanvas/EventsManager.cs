using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace MindCanvas
{
    // 事件管理器
    // 所有的修改都要通过这个东西
    // 已保存历史操作，提供撤销、重做功能
    public static class EventsManager
    {
        private static List<MindCanvasFileData> records = new List<MindCanvasFileData>();// 记录每次操作后的数据
        private static MindMap mindMap;
        private static MindCanvasFile mindCanvasFile;
        private static MindMapCanvas mindMapCanvas;
        private static MindMapInkCanvas mindMapInkCanvas;
        private static bool modified = false;
        private static int nowIndex;

        // 资源加载器，用于翻译
        private static readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

        public static void SetMindMapCanvas(MindMapCanvas newMindMapCanvas)
        {
            mindMapCanvas = newMindMapCanvas;

            mindMapCanvas.MindMap = mindMap;
            mindMapCanvas.DrawAll();
        }

        public static void SetMindMapInkCanvas(MindMapInkCanvas newMindMapInkCanvas)
        {
            mindMapInkCanvas = newMindMapInkCanvas;
            newMindMapInkCanvas.InkPresenter.StrokeContainer = mindMap.InkStrokeContainer;
        }

        public static void Initialize()
        {
            App.mindMap = mindMap = new MindMap();
            App.mindCanvasFile = mindCanvasFile = new MindCanvasFile();

            mindMap.Initialize();
            mindCanvasFile.MindMap = mindMap;
            ClearRecords();
            Record();
            modified = false;
        }

        // 新建文件
        public static async Task<bool> NewFile()
        {
            LogHelper.Info("NewFile");

            if (modified)
            {
                ContentDialogResult result = await Dialog.Show.AskForSave();
                // 用户选择取消
                if (result == ContentDialogResult.None)
                    return false;

                // 用户选择保存
                else if (result == ContentDialogResult.Primary)
                {
                    if (await SaveFile())
                    {
                        Initialize();
                        ResetMainPageCache();
                        RefreshAppTitle();
                        return true;
                    }
                    else
                        return false;// 用户取消了保存
                }
                // 用户选择不保存
                else
                {
                    Initialize();
                    ResetMainPageCache();
                    RefreshAppTitle();
                    return true;
                }
            }

            // 未修改
            else
            {
                Initialize();
                ResetMainPageCache();
                RefreshAppTitle();
                return true;
            }
        }

        private static void ClearRecords()
        {
            nowIndex = -1;
            records.Clear();
        }

        // 打开文件
        public static async Task<bool> OpenFile(StorageFile file)
        {
            LogHelper.Info("OpenFile");

            // 当前文件已修改
            if (modified)
            {
                ContentDialogResult result = await Dialog.Show.AskForSave();

                //用户选择取消
                if (result == ContentDialogResult.None)
                    return false;

                // 用户选择保存
                else if (result == ContentDialogResult.Primary)
                {
                    // 要保存好了才能加载
                    if (await SaveFile())
                    {
                        ClearRecords();
                        if (await mindCanvasFile.LoadFile(file) != MindCanvasFile.LoadFileResult.Success)
                            return false;
                        Record();
                        ResetMainPageCache();
                        RefreshAppTitle();
                        modified = false;
                        return true;
                    }
                    else
                        return false;// 用户取消了保存
                }

                // 用户选择不保存，直接加载
                else if (result == ContentDialogResult.Secondary)
                {
                    ClearRecords();
                    if (await mindCanvasFile.LoadFile(file) != MindCanvasFile.LoadFileResult.Success)
                        return false;
                    Record();
                    ResetMainPageCache();
                    RefreshAppTitle();
                    modified = false;
                    return true;
                }

                // 额应该不存在其他情况，但必须确保一定有返回值
                else
                    return false;
            }

            // 当前文件未修改，直接打开
            else
            {
                ClearRecords();
                if (await mindCanvasFile.LoadFile(file) != MindCanvasFile.LoadFileResult.Success)
                    return false;
                Record();
                ResetMainPageCache();
                RefreshAppTitle();
                modified = false;
                return true;
            }
        }

        // 退出应用
        public static async void CloseRequested()
        {
            // 当前文件已修改
            if (modified)
            {
                ContentDialogResult result = await Dialog.Show.AskForSave();

                //用户选择取消
                if (result == ContentDialogResult.None)
                    return;

                // 用户选择保存
                else if (result == ContentDialogResult.Primary)
                {
                    // 要保存好了才能退出
                    if (await SaveFile())
                        Application.Current.Exit();
                    else
                        return;// 用户取消了保存
                }

                // 用户选择不保存，直接退出
                else if (result == ContentDialogResult.Secondary)
                    Application.Current.Exit();
            }

            // 当前文件未修改，直接退出
            else
                Application.Current.Exit();
        }

        // 清除主页面缓存
        public static void ResetMainPageCache()
        {
            if (MainPage.mainPage != null)
                MainPage.mainPage.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Disabled;
        }

        // 保存文件
        public static async Task<bool> SaveFile()
        {
            LogHelper.Info("SaveFile");

            if (mindCanvasFile.File != null)
            {
                await mindCanvasFile.SaveFile();
                ResetMainPageCache();
                modified = false;
                return true;
            }
            else
            {
                var savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

                // Dropdown of file types the user can save the file as
                string fileTypeName = resourceLoader.GetString("Code_MindCanvasFile");// MindCanvas 文件
                savePicker.FileTypeChoices.Add(fileTypeName, new List<string>() { ".mindcanvas" });

                // Default file name if the user does not type one in or select a file to replace
                string suggestedFileName = resourceLoader.GetString("Code_Untitled");// 无标题
                savePicker.SuggestedFileName = suggestedFileName;

                StorageFile file = await savePicker.PickSaveFileAsync();

                if (file != null)
                {
                    mindCanvasFile.File = file;
                    await mindCanvasFile.SaveFile();
                    RefreshAppTitle();
                    ResetMainPageCache();
                    modified = false;
                    return true;
                }
                else
                    return false;
            }
        }

        // 修改点的名称和描述
        public static void ModifyNode(Node node, string newName, string newDescription)
        {
            NodeControl border = mindMapCanvas.ConvertNodeToBorder(node);

            border.Text = newName;

            mindMap.ModifyNode(node.Id, newName, newDescription);
            Record();
        }

        // 修改点的坐标
        public static void ModifyNode(Node node, double x, double y)
        {
            mindMap.ModifyNode(node.Id, x, y);

            NodeControl border = mindMapCanvas.ConvertNodeToBorder(node);
            Canvas.SetTop(border, mindMapCanvas.Height / 2 + y - border.ActualHeight / 2);
            Canvas.SetLeft(border, mindMapCanvas.Width / 2 + x - border.ActualWidth / 2);
            mindMapCanvas.ReDrawTies(node);

            Record();
        }

        // 修改点的边框颜色
        public static void ModifyNodeBorderBrushColor(Node node, Color? borderBrushColor)
        {
            NodeControl border = mindMapCanvas.ConvertNodeToBorder(node);
            if (borderBrushColor != null)
            {
                SolidColorBrush borderBrush = new SolidColorBrush(borderBrushColor.Value);
                border.BorderBrush = borderBrush;
                node.BorderBrush = borderBrush;
            }
            else
            {
                node.BorderBrush = null;
                border.BorderBrush = mindMap.DefaultNodeBorderBrush;
            }

            Record();
        }

        // 修改点的字体大小
        public static void ModifyNodeNameFontSize(Node node, double? nameFontSize)
        {
            NodeControl nodeControl = mindMapCanvas.ConvertNodeToBorder(node);

            node.NameFontSize = nameFontSize;

            if (nameFontSize != null)
                nodeControl.FontSize = nameFontSize.Value;
            else
                nodeControl.FontSize = mindMap.DefaultNodeNameFontSize;

            Record();
        }

        // 修改点的样式
        public static void ModifyNodeStyle(Node node, string style)
        {
            NodeControl nodeControl = mindMapCanvas.ConvertNodeToBorder(node);

            node.Style = style;
            nodeControl.Style = style;
            


            Record();
        }

        // 修改线的描述
        public static void ModifyTie(Tie tie, string newDescription)
        {
            mindMap.ModifyTie(tie.Id, newDescription);
            Record();
        }

        // 新建点
        public static Node AddNode(string name, string description = "")
        {
            LogHelper.Info("AddNode");

            Node newNode = mindMap.AddNode(name, description);
            mindMapCanvas.Draw(newNode);
            Record();
            return newNode;
        }

        // 新建线
        public static Tie AddTie(Node node1, Node node2, string description = "")
        {
            LogHelper.Info("AddTie");

            Tie newTie = mindMap.AddTie(node1.Id, node2.Id, description);
            mindMapCanvas.Draw(newTie);
            Record();
            return newTie;
        }

        // 删除点
        public static void RemoveNode(Node node)
        {
            foreach (Tie tie in mindMap.GetTies(node))
                mindMapCanvas.Clear(tie);

            mindMap.RemoveNode(node);
            mindMapCanvas.Clear(node);
            Record();
        }

        // 删除所有点
        public static void RemoveAllNodes()
        {
            if (mindMap.Nodes.Count() == 0)
                return;

            mindMap.Ties.Clear();
            mindMap.Nodes.Clear();
            mindMapCanvas.ReDraw();
            Record();
        }

        // 删除线
        public static void RemoveTie(Tie tie)
        {
            mindMap.RemoveTie(tie);
            mindMapCanvas.Clear(tie);
            Record();
        }

        // 删除所有线
        public static void RemoveAllTies()
        {
            if (mindMap.Ties.Count() == 0)
                return;

            mindMap.Ties.Clear();
            mindMapCanvas.ReDraw();
            Record();
        }

        // 修改墨迹
        public static void ModifyInkCanvas(InkStrokeContainer newInkStrokeContainer)
        {
            mindMap.InkStrokeContainer = newInkStrokeContainer;
            Record();
        }

        // 修改默认值
        public static void ModifyDefaultSettings(Color defaultNodeBorderBrushColor, double defaultNodeNameFontSize)
        {
            App.mindMap.DefaultNodeBorderBrush = new SolidColorBrush(defaultNodeBorderBrushColor);
            App.mindMap.DefaultNodeNameFontSize = defaultNodeNameFontSize;
            Record();
        }

        // 修改可视区域
        public static void ModifyViewport(double? visualCenterX, double? visualCenterY, float? zoomFactor)
        {
            if (visualCenterX != null)
                App.mindMap.VisualCenterX = visualCenterX.Value;
            if (visualCenterY != null)
                App.mindMap.VisualCenterY = visualCenterY.Value;
            if (zoomFactor != null)
                App.mindMap.ZoomFactor = zoomFactor.Value;
        }

        // 设置父节点
        public static void SetParentNode(Node node, Node parentNode)
        {
            if (parentNode == null && node.ParentNodeId.HasValue)
            {
                node.ParentNodeId = null;
                Record();
            }

            else if (parentNode != null && node.ParentNodeId != parentNode.Id)
            {
                node.ParentNodeId = parentNode.Id;
                Record();
            }
        }

        // 整理点
        public static async Task Tidy(List<Node> nodes)
        {
            await LoadingHelper.ShowLoading("正在整理……");

            // 计时器
            Stopwatch sw = new Stopwatch();
            sw.Start();

            foreach (Node node in nodes)
                await TidyTask(node);

            // 获取用时
            sw.Stop();
            LogHelper.Debug(sw.Elapsed);

            mindMapCanvas.ReDraw();
            Record();

            LoadingHelper.HideLoading();

            InfoHelper.ShowInfoBar("整理完成", InfoBarSeverity.Success);
        }

        // 整理点
        public static Task<bool> TidyTask(Node parentNode)
        {
            var task = Task.Run(() =>
            {
                List<Node> childrenNode = new List<Node>();// 子节点
                List<Node> leftNodes = new List<Node>();// 在父节点左侧的点
                List<Node> rightNodes = new List<Node>();// 在父节点右侧的点
                List<Node> parentNodes = new List<Node>();// 是父节点的子节点

                childrenNode = mindMap.GetChildren(parentNode);
                if (childrenNode.Count == 0)
                    return false;

                foreach (Node childNode in childrenNode)
                {
                    if (childNode.X < parentNode.X)
                        leftNodes.Add(childNode);
                    else
                        rightNodes.Add(childNode);

                    if (mindMap.GetChildren(childNode).Count > 0)
                        parentNodes.Add(childNode);
                }

                // 在y轴上排序
                leftNodes.Sort((node1, node2) => node1.Y.CompareTo(node2.Y));
                rightNodes.Sort((node1, node2) => node1.Y.CompareTo(node2.Y));

                // 安排位置
                double spacingY = 70;
                double spacingX = 200;

                double minY;
                double y;

                // 左侧
                minY = parentNode.Y - leftNodes.Count / 2 * spacingY;
                if (leftNodes.Count % 2 == 0)
                    minY += spacingY / 2;

                y = minY;
                for (int i = 0; i < leftNodes.Count; i++)
                {
                    Node leftNode = leftNodes[i];
                    leftNode.X = parentNode.X - spacingX;
                    leftNode.Y = y;
                    y += spacingY;
                }

                // 右侧
                minY = parentNode.Y - rightNodes.Count / 2 * spacingY;
                if (rightNodes.Count % 2 == 0)
                    minY += spacingY / 2;

                y = minY;
                for (int i = 0; i < rightNodes.Count; i++)
                {
                    Node rightNode = rightNodes[i];
                    rightNode.X = parentNode.X + spacingX;
                    rightNode.Y = y;
                    y += spacingY;
                }

                return true;
            });
            return task;
        }

        // 撤销
        public static void Undo()
        {
            MindCanvasFileData lastData = DeepCopy(records[--nowIndex]);

            // 视觉数据不变
            lastData.VisualCenterX = mindMap.VisualCenterX;
            lastData.VisualCenterY = mindMap.VisualCenterY;
            lastData.ZoomFactor = mindMap.ZoomFactor;

            mindMap.LoadData(lastData);
            mindMapCanvas.ShowAnimation = false;
            mindMapCanvas.ReDraw();// 因为mindMapCanvas已与mindMap绑定，所以只需ReDraw刷新即可
            mindMapInkCanvas.InkPresenter.StrokeContainer = lastData.InkStrokeContainer;// 墨迹
            mindMapCanvas.ShowAnimation = true;
            modified = true;
        }

        // 判断能否撤销
        public static bool CanUndo
        {
            get
            {
                return nowIndex > 0;
            }
        }

        // 重做
        public static void Redo()
        {
            MindCanvasFileData nextData = DeepCopy(records[++nowIndex]);

            // 视觉数据不变
            nextData.VisualCenterX = mindMap.VisualCenterX;
            nextData.VisualCenterY = mindMap.VisualCenterY;
            nextData.ZoomFactor = mindMap.ZoomFactor;

            mindMap.LoadData(nextData);
            mindMapCanvas.ShowAnimation = false;
            mindMapCanvas.ReDraw();// 因为mindMapCanvas已与mindMap绑定，所以只需ReDraw刷新即可
            mindMapInkCanvas.InkPresenter.StrokeContainer = nextData.InkStrokeContainer;// 墨迹
            mindMapCanvas.ShowAnimation = true;
            modified = true;
        }

        // 判断能否重做
        public static bool CanRedo
        {
            get
            {
                return records.Count() > nowIndex + 1;
            }
        }

        // 记录一下
        private static void Record()
        {
            // 判断现在是不是在records列表的最后一项操作，是的话直接添加记录就好
            // 如果不是的话，要删除之后的，再添加（类似于新分支）
            // 如果能撤消，说明没在最后一项操作
            if (CanRedo)
                records.RemoveRange(nowIndex + 1, records.Count() - (nowIndex + 1));
            records.Add(DeepCopy(mindMap.GetData()));
            nowIndex++;
            modified = true;
        }

        // 深拷贝
        private static T DeepCopy<T>(T obj)
        {
            object retval;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                //序列化成流
                bf.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);
                //反序列化成对象
                retval = bf.Deserialize(ms);
                ms.Close();
            }
            return (T)retval;
        }

        // 刷新标题
        private static void RefreshAppTitle()
        {
            string fileName = mindCanvasFile.File?.DisplayName ?? "无标题";
            AppTitleBarControl.SetFileName(fileName);
        }

        // 搜索思维导图
        public static List<Item> SearchMindMap(string queryText)
        {
            List<Item> result = new List<Item>();
            string[] keywords = queryText.ToLower().Split(" ");

            // 在点中查找
            foreach (Node node in App.mindMap.Nodes)
            {
                bool flag = true;
                string itemText = $"Node: {node.Name}";
                foreach (string word in keywords)
                    if (!itemText.ToLower().Contains(word.ToLower()) && !node.Description.ToLower().Contains(word.ToLower()))
                        flag = false;

                if (flag)
                    result.Add(new Item(itemText, tag: node.Id));
            }

            // 在线中查找
            foreach (Tie tie in App.mindMap.Ties)
            {
                bool flag = true;
                List<Node> nodes = App.mindMap.GetNodes(tie);
                string itemText = $"Tie: {nodes[0].Name} <-> {nodes[1].Name}";
                foreach (string word in keywords)
                    if (!tie.Description.ToLower().Contains(word.ToLower()) && !itemText.ToLower().Contains(word.ToLower()))
                        flag = false;

                if (flag)
                    result.Add(new Item(itemText, tag: tie.Id));
            }

            return result;
        }
    }
}
