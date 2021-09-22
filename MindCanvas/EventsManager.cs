using System;
using System.Collections.Generic;
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
    /// <summary>
    /// 事件管理器
    /// 所有的修改都要通过这个东西
    /// 保存历史操作，提供撤销、重做功能
    /// </summary>
    public static class EventsManager
    {
        private static List<MindCanvasFileData> records = new List<MindCanvasFileData>();// 记录每次操作后的数据
        private static bool modified = false;
        private static int nowIndex;

        // 资源加载器，用于翻译
        private static readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

        public static void SetMindMapCanvas(MindMapCanvas newMindMapCanvas)
        {
            newMindMapCanvas.MindMap = App.mindMap;
            newMindMapCanvas.DrawAll();
        }

        public static void SetMindMapInkCanvas(MindMapInkCanvas newMindMapInkCanvas)
        {
            newMindMapInkCanvas.InkPresenter.StrokeContainer = App.mindMap.InkStrokeContainer;
        }

        public static void Initialize()
        {
            App.mindMap = new MindMap();
            App.mindCanvasFile = new MindCanvasFile();

            App.mindMap.Initialize();
            App.mindCanvasFile.MindMap = App.mindMap;
            ClearRecords();
            Record();
            modified = false;
        }

        /// <summary>新建文件</summary>
        public static async Task<bool> NewFile()
        {
            LogHelper.Info("NewFile");

            if (modified)
            {
                ContentDialogResult result = await Dialog.Show.AskForSave();

                switch (result)
                {
                    // 用户选择取消
                    case ContentDialogResult.None:
                        return false;
                    case ContentDialogResult.Primary:
                        if (await SaveFile())
                        {
                            Initialize();
                            ResetMainPageCache();
                            RefreshAppTitle();
                            return true;
                        }
                        else
                            return false;// 用户取消了保存
                    default:
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

        /// <summary>打开文件</summary>
        public static async Task<bool> OpenFile(StorageFile file)
        {
            LogHelper.Info("OpenFile");

            // 当前文件已修改
            if (modified)
            {
                ContentDialogResult result = await Dialog.Show.AskForSave();

                //用户选择取消
                switch (result)
                {
                    case ContentDialogResult.None:
                        return false;
                    case ContentDialogResult.Primary:
                        // 要保存好了才能加载
                        if (await SaveFile())
                        {
                            ClearRecords();
                            if (await App.mindCanvasFile.LoadFile(file) != MindCanvasFile.LoadFileResult.Success)
                                return false;
                            Record();
                            ResetMainPageCache();
                            RefreshAppTitle();
                            modified = false;
                            return true;
                        }
                        else
                            return false;// 用户取消了保存
                    case ContentDialogResult.Secondary:
                        ClearRecords();
                        if (await App.mindCanvasFile.LoadFile(file) != MindCanvasFile.LoadFileResult.Success)
                            return false;
                        Record();
                        ResetMainPageCache();
                        RefreshAppTitle();
                        modified = false;
                        return true;
                    default:
                        return false;
                }
            }

            // 当前文件未修改，直接打开
            else
            {
                ClearRecords();
                if (await App.mindCanvasFile.LoadFile(file) != MindCanvasFile.LoadFileResult.Success)
                    return false;
                Record();
                ResetMainPageCache();
                RefreshAppTitle();
                modified = false;
                return true;
            }
        }

        /// <summary>退出应用</summary>
        public static async void CloseRequested()
        {
            // 当前文件已修改
            if (modified)
            {
                ContentDialogResult result = await Dialog.Show.AskForSave();

                //用户选择取消
                switch (result)
                {
                    case ContentDialogResult.None:
                        return;
                    case ContentDialogResult.Primary:
                        // 要保存好了才能退出
                        if (await SaveFile())
                            Application.Current.Exit();
                        else
                            return;// 用户取消了保存
                        break;
                    case ContentDialogResult.Secondary:
                        Application.Current.Exit();
                        break;
                }
            }

            // 当前文件未修改，直接退出
            else
                Application.Current.Exit();
        }

        /// <summary>清除主页面缓存</summary>
        public static void ResetMainPageCache()
        {
            if (MainPage.mainPage != null)
                MainPage.mainPage.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Disabled;
        }

        /// <summary>保存文件</summary>
        public static async Task<bool> SaveFile()
        {
            LogHelper.Info("SaveFile");

            if (App.mindCanvasFile.File != null)
            {
                await App.mindCanvasFile.SaveFile();
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
                    App.mindCanvasFile.File = file;
                    await App.mindCanvasFile.SaveFile();
                    RefreshAppTitle();
                    modified = false;
                    return true;
                }
                else
                    return false;
            }
        }

        /// <summary>修改点的名称和描述</summary>
        public static void ModifyNode(Node node, string newName, string newDescription)
        {
            NodeControl border = MainPage.mindMapCanvas.ConvertNodeToBorder(node);

            border.Text = newName;

            App.mindMap.ModifyNode(node.Id, newName, newDescription);
            Record();
        }

        /// <summary>修改点的坐标</summary>
        public static void ModifyNode(Node node, double x, double y)
        {
            App.mindMap.ModifyNode(node.Id, x, y);

            NodeControl border = MainPage.mindMapCanvas.ConvertNodeToBorder(node);
            Canvas.SetTop(border, MainPage.mindMapCanvas.Height / 2 + y - border.ActualHeight / 2);
            Canvas.SetLeft(border, MainPage.mindMapCanvas.Width / 2 + x - border.ActualWidth / 2);
            MainPage.mindMapCanvas.ReDrawTies(node);

            Record();
        }

        /// <summary>修改点的边框颜色</summary>
        public static void ModifyNodeBorderBrushColor(Node node, Color? borderBrushColor)
        {
            NodeControl border = MainPage.mindMapCanvas.ConvertNodeToBorder(node);
            if (borderBrushColor.HasValue)
            {
                SolidColorBrush borderBrush = new SolidColorBrush(borderBrushColor.Value);
                border.BorderBrush = borderBrush;
                node.BorderBrush = borderBrush;
            }
            else
            {
                node.BorderBrush = null;
                border.BorderBrush = App.mindMap.DefaultNodeBorderBrush;
            }

            Record();
        }

        /// <summary>修改点的字体大小</summary>
        public static void ModifyNodeNameFontSize(Node node, double? nameFontSize)
        {
            NodeControl nodeControl = MainPage.mindMapCanvas.ConvertNodeToBorder(node);

            node.NameFontSize = nameFontSize;

            nodeControl.FontSize = nameFontSize != null ? nameFontSize.Value : App.mindMap.DefaultNodeNameFontSize;

            Record();
        }

        /// <summary>修改点的样式</summary>
        public static void ModifyNodeStyle(Node node, string style)
        {
            NodeControl nodeControl = MainPage.mindMapCanvas.ConvertNodeToBorder(node);

            node.Style = style;

            nodeControl.Style = style ?? App.mindMap.DefaultNodeStyle;

            foreach (Tie tie in App.mindMap.GetTies(node))
            {
                MainPage.mindMapCanvas.ReDraw(tie);
                MainPage.mainPage.ConfigTiesPath(new List<Tie> { tie });
            }


            Record();
        }

        /// <summary>修改线的描述</summary>
        public static void ModifyTie(Tie tie, string newDescription)
        {
            App.mindMap.ModifyTie(tie.Id, newDescription);
            Record();
        }

        /// <summary>修改线的颜色</summary>
        public static void ModifyTieStrokeColor(Tie tie, Color? strokeColor)
        {
            Windows.UI.Xaml.Shapes.Path path = MainPage.mindMapCanvas.ConvertTieToPath(tie);
            if (strokeColor.HasValue)
            {
                SolidColorBrush stroke = new SolidColorBrush(strokeColor.Value);
                path.Stroke = stroke;
                tie.Stroke = stroke;
            }

            else
            {
                tie.Stroke = null;
                path.Stroke = App.mindMap.DefaultTieStroke;
            }

            Record();
        }

        /// <summary>新建点</summary>
        public static Node AddNode(string name, string description = "")
        {
            LogHelper.Info("AddNode");

            Node newNode = App.mindMap.AddNode(name, description);
            MainPage.mindMapCanvas.Draw(newNode);
            Record();
            return newNode;
        }

        /// <summary>新建线</summary>
        public static Tie AddTie(Node node1, Node node2, string description = "")
        {
            LogHelper.Info("AddTie");

            Tie newTie = App.mindMap.AddTie(node1.Id, node2.Id, description);
            MainPage.mindMapCanvas.Draw(newTie);
            Record();
            return newTie;
        }

        /// <summary>删除点</summary>
        public static void RemoveNode(Node node)
        {
            foreach (Tie tie in App.mindMap.GetTies(node))
                MainPage.mindMapCanvas.Clear(tie);

            App.mindMap.RemoveNode(node);
            MainPage.mindMapCanvas.Clear(node);
            Record();
        }

        /// <summary>删除所有点</summary>
        public static void RemoveAllNodes()
        {
            if (App.mindMap.Nodes.Count() == 0)
                return;

            App.mindMap.Ties.Clear();
            App.mindMap.Nodes.Clear();
            MainPage.mindMapCanvas.ReDraw();
            Record();
        }

        /// <summary>删除线</summary>
        public static void RemoveTie(Tie tie)
        {
            App.mindMap.RemoveTie(tie);
            MainPage.mindMapCanvas.Clear(tie);
            Record();
        }

        /// <summary>删除所有线</summary>
        public static void RemoveAllTies()
        {
            if (App.mindMap.Ties.Count() == 0)
                return;

            App.mindMap.Ties.Clear();
            MainPage.mindMapCanvas.ReDraw();
            Record();
        }

        /// <summary>修改墨迹</summary>
        public static void ModifyInkCanvas(InkStrokeContainer newInkStrokeContainer)
        {
            App.mindMap.InkStrokeContainer = newInkStrokeContainer;
            Record();
        }

        /// <summary>修改默认值</summary>
        public static void ModifyDefaultSettings(Color defaultNodeBorderBrushColor,
                                                 double defaultNodeNameFontSize,
                                                 string defaultNodeStyle,
                                                 Color defaultTieStrokeColor)
        {
            App.mindMap.DefaultNodeBorderBrush = new SolidColorBrush(defaultNodeBorderBrushColor);
            App.mindMap.DefaultNodeNameFontSize = defaultNodeNameFontSize;
            App.mindMap.DefaultNodeStyle = defaultNodeStyle;
            App.mindMap.DefaultTieStroke = new SolidColorBrush(defaultTieStrokeColor);

            Record();
        }

        /// <summary>修改可视区域</summary>
        public static void ModifyViewport(double? visualCenterX, double? visualCenterY, float? zoomFactor)
        {
            if (visualCenterX != null)
                App.mindMap.VisualCenterX = visualCenterX.Value;
            if (visualCenterY != null)
                App.mindMap.VisualCenterY = visualCenterY.Value;
            if (zoomFactor != null)
                App.mindMap.ZoomFactor = zoomFactor.Value;
        }

        /// <summary>撤销</summary>
        public static void Undo()
        {
            MindCanvasFileData lastData = DeepCopy(records[--nowIndex]);

            // 视觉数据不变
            lastData.VisualCenterX = App.mindMap.VisualCenterX;
            lastData.VisualCenterY = App.mindMap.VisualCenterY;
            lastData.ZoomFactor = App.mindMap.ZoomFactor;

            App.mindMap.LoadData(lastData);
            MainPage.mindMapCanvas.ShowAnimation = false;
            MainPage.mindMapCanvas.ReDraw();// 因为mindMapCanvas已与mindMap绑定，所以只需ReDraw刷新即可
            MainPage.mindMapInkCanvas.InkPresenter.StrokeContainer = lastData.InkStrokeContainer;// 墨迹
            MainPage.mindMapCanvas.ShowAnimation = true;
            modified = true;
        }

        /// <summary>判断能否撤销</summary>
        public static bool CanUndo => nowIndex > 0;

        /// <summary>重做</summary>
        public static void Redo()
        {
            MindCanvasFileData nextData = DeepCopy(records[++nowIndex]);

            // 视觉数据不变
            nextData.VisualCenterX = App.mindMap.VisualCenterX;
            nextData.VisualCenterY = App.mindMap.VisualCenterY;
            nextData.ZoomFactor = App.mindMap.ZoomFactor;

            App.mindMap.LoadData(nextData);
            MainPage.mindMapCanvas.ShowAnimation = false;
            MainPage.mindMapCanvas.ReDraw();// 因为mindMapCanvas已与mindMap绑定，所以只需ReDraw刷新即可
            MainPage.mindMapInkCanvas.InkPresenter.StrokeContainer = nextData.InkStrokeContainer;// 墨迹
            MainPage.mindMapCanvas.ShowAnimation = true;
            modified = true;
        }

        /// <summary>判断能否重做</summary>
        public static bool CanRedo => records.Count() > nowIndex + 1;

        /// <summary>记录一下</summary>
        private static void Record()
        {
            // 判断现在是不是在records列表的最后一项操作，是的话直接添加记录就好
            // 如果不是的话，要删除之后的，再添加（类似于新分支）
            // 如果能撤消，说明没在最后一项操作
            if (CanRedo)
                records.RemoveRange(nowIndex + 1, records.Count() - (nowIndex + 1));
            records.Add(DeepCopy(App.mindMap.GetData()));
            nowIndex++;
            modified = true;
        }

        /// <summary>深拷贝</summary>
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

        /// <summary>刷新标题</summary>
        public static void RefreshAppTitle()
        {
            string fileName = App.mindCanvasFile.File?.DisplayName ?? resourceLoader.GetString("Code_Untitled");// 无标题
            AppTitleBarControl.SetFileName(fileName);
        }

        /// <summary>搜索思维导图</summary>
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
                    if (!itemText.Contains(word, StringComparison.CurrentCultureIgnoreCase) && !node.Description.Contains(word, StringComparison.CurrentCultureIgnoreCase))
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
                    if (!tie.Description.Contains(word, StringComparison.CurrentCultureIgnoreCase) && !itemText.Contains(word, StringComparison.CurrentCultureIgnoreCase))
                        flag = false;

                if (flag)
                    result.Add(new Item(itemText, tag: tie.Id));
            }

            return result;
        }
    }
}
