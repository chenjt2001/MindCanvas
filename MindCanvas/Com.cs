using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml;
using Windows.Storage;
using Windows.Storage.Streams;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml.Media.Animation;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using Windows.UI.Xaml.Input;
using Windows.UI.Input;

namespace MindCanvas
{

    [Serializable]// 标记为可序列化
    public class Node
    {
        public int id;
        public string description;

        // 兼容多版本
        [OptionalField]
        public string name;
        [OptionalField]
        public string title;

        // 相对于画布中心的位置
        public double x;
        public double y;
    };

    [Serializable]
    public class Tie
    {
        public int id;
        public string description;
        public int node1id;
        public int node2id;
    }

    [Serializable]
    public struct MindCanvasFileData
    {
        public List<Node> nodes;
        public List<Tie> ties;
    }

    // 思维导图文件
    public class MindCanvasFile
    {
        private MindMap mindMap;
        private readonly StorageItemMostRecentlyUsedList mru = StorageApplicationPermissions.MostRecentlyUsedList;
        public StorageFile file;

        // 初始化
        public MindCanvasFile()
        {
        }

        // 加载数据
        public void SetMindMap(MindMap mindMap)
        {
            this.mindMap = mindMap;
        }

        // 获取数据
        public MindMap GetMindMap()
        {
            return mindMap;
        }

        // 保存文件
        public async void SaveFile(bool addToMru = true)
        {
            MindCanvasFileData inMindFileData = mindMap.GetData();

            using (MemoryStream ms = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, inMindFileData);
                await FileIO.WriteBytesAsync(file, ms.GetBuffer());
            }

            // 将文件添加到MRU（最近访问列表）
            if (addToMru)
                mru.Add(file);
        }

        // 加载文件
        public async Task LoadFile(StorageFile storageFile, bool addToMru = true)
        {
            file = storageFile;
            IBuffer buffer = await FileIO.ReadBufferAsync(file);

            using (var dataReader = DataReader.FromBuffer(buffer))
            {
                var bytes = new byte[buffer.Length];
                dataReader.ReadBytes(bytes);

                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    IFormatter formatter = new BinaryFormatter();
                    MindCanvasFileData inMindFileData = (MindCanvasFileData)formatter.Deserialize(ms);
                    VersionHelper(ref inMindFileData);

                    mindMap.Load(inMindFileData.nodes, inMindFileData.ties);
                    if (addToMru)
                        mru.Add(file);
                }
            }
        }

        // 兼容版本
        private void VersionHelper(ref MindCanvasFileData data)
        {
            // 兼容第一版（发布版本号为1.0.2.0）
            foreach (Node node in data.nodes)
                if (node.title != null && node.name == null)
                {
                    node.name = node.title;
                }
                    
        }
    }

    // 思维导图Canvas
    public class MindMapCanvas : Canvas// InfiniteCanvas
    {
        private MindMap mindMap;
        private Dictionary<int, Border> nodeIdBorder = new Dictionary<int, Border>();// 标记点id与border
        private Dictionary<int, Windows.UI.Xaml.Shapes.Path> tieIdPath = new Dictionary<int, Windows.UI.Xaml.Shapes.Path>();// 标记线id与path
        private bool showAnimation;// 是否显示动画

        public MindMapCanvas(bool showAnimation = true)
        {
            mindMap = new MindMap();
            VerticalAlignment = VerticalAlignment.Center;
            HorizontalAlignment = HorizontalAlignment.Center;
            //Background = new SolidColorBrush(Colors.White);

            // 设置动画
            this.showAnimation = showAnimation;
            if (showAnimation)
            {
                TransitionCollection tc = new TransitionCollection() { };
                tc.Add(new EntranceThemeTransition() { IsStaggeringEnabled = true });
                ChildrenTransitions = tc;
            }

            // 画布大小
            Width = 100000;
            Height = 100000;
        }


        // 设置此Canvas对应的思维导图
        public void SetMindMap(MindMap mindMap)
        {
            this.mindMap = mindMap;
        }

        // 获取此Canvas对应的思维导图
        public MindMap GetMindMap()
        {
            return mindMap;
        }

        // 画一个点
        public void Draw(Node node)
        {
            var background = new SolidColorBrush((Color)XamlBindingHelper.ConvertValue(typeof(Color), "White"));
            var borderBrush = new SolidColorBrush((Color)XamlBindingHelper.ConvertValue(typeof(Color), "#FF0000FF"));//蓝色
            var foreground = new SolidColorBrush(Colors.Black);
            var borderThickness = new Thickness(2);
            var padding = new Thickness(10);
            var cornerRadius = new CornerRadius(5);

            Border border = new Border()// 也可以使用Rectangle
            {
                Background = background,// 背景颜色
                BorderBrush = borderBrush,// 边框颜色
                BorderThickness = borderThickness,// 边框的粗细
                Padding = padding,// 边框与子对象间的距离
                CornerRadius = cornerRadius,// 圆角半径
            };

            TextBlock textBlock = new TextBlock()
            {
                Text = node.name,// 文字内容
                FontSize = 20,// 文字大小
                Foreground = foreground// 文字颜色
            };

            SetTop(border, Height / 2 + node.y);
            SetLeft(border, Width / 2 + node.x);

            border.Child = textBlock;
            Children.Add(border);

            nodeIdBorder[node.id] = border;
        }

        // 画一条线
        public void Draw(Tie tie)
        {
            // 获取这条线的两个点
            List<Node> nodes = mindMap.GetNodes(tie);
            Node node1 = nodes[0];
            Node node2 = nodes[1];
            Border node1Border = ConvertNodeToBorder(node1);
            Border node2Border = ConvertNodeToBorder(node2);

            // 绘制Path
            SolidColorBrush stroke = new SolidColorBrush(Colors.Gray);
            Windows.UI.Xaml.Shapes.Path path = new Windows.UI.Xaml.Shapes.Path()
            {
                Stroke = stroke,// 线条颜色
                StrokeThickness = 3,// 线条粗细
            };

            // https://docs.microsoft.com/zh-cn/windows/uwp/xaml-platform/move-draw-commands-syntax
            // 移动和绘制命令语法
            // 起点和三次方贝塞尔曲线
            // 三次方贝塞尔曲线命令:C controlPoint1 controlPoint2 endPoint
            // controlPoint1: 曲线的第一个控制点，它决定曲线的起始切线
            // controlPoint2: 曲线的第二个控制点，它决定曲线的结束切线。
            // endPoint: 终结点，绘制曲线将通过的点
            string commands = string.Format("M {0} {1} C {4} {1} {4} {3} {2} {3}",
                Width / 2 + node1.x + node1Border.ActualWidth / 2,
                Height / 2 + node1.y + node1Border.ActualHeight / 2,
                Width / 2 + node2.x + node2Border.ActualWidth / 2,
               Height / 2 + node2.y + node2Border.ActualHeight / 2,
                (Width / 2 + node1.x + node1Border.ActualWidth / 2 + Width / 2 + node2.x + node2Border.ActualWidth / 2) / 2);
            var pathData = (Geometry)(XamlBindingHelper.ConvertValue(typeof(Geometry), commands));
            path.Data = pathData;

            SetZIndex(path, -10000);// 确保线在点下面
            Children.Add(path);

            tieIdPath[tie.id] = path;
        }

        // 查询border对应的点
        public Node ConvertBorderToNode(Border border)
        {
            Node requiredNode = new Node();
            int id = 0;

            foreach (int i in nodeIdBorder.Keys)
                if (nodeIdBorder[i] == border)
                {
                    id = i;
                    break;
                }

            foreach (Node node in mindMap.nodes)
                if (node.id == id)
                {
                    requiredNode = node;
                    break;
                }

            return requiredNode;
        }

        // 查询点对应的Border
        public Border ConvertNodeToBorder(Node node) {
            return nodeIdBorder[node.id]; 
        }
            

        // 查询path对应的线
        public Tie ConvertPathToTie(Windows.UI.Xaml.Shapes.Path path)
        {
            Tie requiredTie = new Tie();
            int id = 0;

            foreach (int i in tieIdPath.Keys)
                if (tieIdPath[i] == path)
                {
                    id = i;
                    break;
                }

            foreach (Tie tie in mindMap.ties)
                if (tie.id == id)
                {
                    requiredTie = tie;
                    break;
                }

            return requiredTie;
        }

        // 查询线对应的Path
        public Windows.UI.Xaml.Shapes.Path ConvertTieToPath(Tie tie)
        {
            return tieIdPath[tie.id];
        }

        // 绘制全部
        public void DrawAll()
        {
            // 绘制点
            foreach (Node node in mindMap.nodes)
                Draw(node);
            UpdateLayout();//渲染canvas，确保线正确获取点的位置
            // 绘制线
            foreach (Tie tie in mindMap.ties)
                Draw(tie);
        }

        // 清除点
        public void Clear(Node node)
        {
            Border border = ConvertNodeToBorder(node);
            Children.Remove(border);
        }

        // 清除连接
        public void Clear(Tie tie)
        {
            Windows.UI.Xaml.Shapes.Path path = ConvertTieToPath(tie);
            Children.Remove(path);
        }

        // 重绘
        public void ReDraw()
        {
            Children.Clear();
            nodeIdBorder.Clear();
            tieIdPath.Clear();
            DrawAll();
        }

        public void ReDraw(Tie tie)
        {
            Children.Remove(ConvertTieToPath(tie));
            UpdateLayout();// 确保获取到了tie连着的两个node对应的border的ActualWidth和ActualHeight
            Draw(tie);
        }


        // 修改线的path
        public void ModifyTiePath(Tie tie, string commands)
        {
            Windows.UI.Xaml.Shapes.Path path = ConvertTieToPath(tie);
            var pathData = (Geometry)(XamlBindingHelper.ConvertValue(typeof(Geometry), commands));
            path.Data = pathData;
            UpdateLayout();
        }
    }

    // 思维导图
    public class MindMap
    {
        public List<Node> nodes;
        public List<Tie> ties;

        public MindMap()
        {
            nodes = new List<Node>();
            ties = new List<Tie>();
        }

        // 初始化成新的思维导图
        public void Initialize()
        {
            RemoveAll();

            Node firstNode = new Node
            {
                id = 0,
                name = "主节点",
                description = "第一个节点",
                x = 0,
                y = 0,
            };

            nodes.Add(firstNode);
        }

        // 添加点
        public Node AddNode(string name, string description = "暂无描述")
        {
            Node newNode = new Node
            {
                id = nodes.Last().id + 1,
                name = name,
                description = description,
                x = nodes.Last().x + 20,
                y = nodes.Last().y + 20,
            };

            nodes.Add(newNode);
            return newNode;
        }

        // 添加连接
        public Tie AddTie(int node1id, int node2id, string description = "暂无描述")
        {
            int id;
            if (ties.Count() == 0)
                 id = 0;
            else
                id = ties.Last().id + 1;

            Tie newTie = new Tie
            {
                id = id,
                description = description,
                node1id = node1id,
                node2id = node2id,
            };

            ties.Add(newTie);
            return newTie;
        }

        // 获取一条线连接的两个点
        public List<Node> GetNodes(Tie tie)
        {
            List<Node> needNodes = new List<Node>();

            // 按id查找要连接的两个点
            foreach (Node node in nodes)
                if (node.id == tie.node1id)
                    needNodes.Add(node);
                else if (node.id == tie.node2id)
                    needNodes.Add(node);

            return needNodes;
        }

        // 获取连着一个点的线有哪些
        public List<Tie> GetTies(Node node)
        {
            List<Tie> needTies = new List<Tie>();
            foreach (Tie tie in ties)
                if (node.id == tie.node1id || node.id == tie.node2id)
                    needTies.Add(tie);
            return needTies;
        }

        // 按Id获取点
        public Node GetNode(int id)
        {
            foreach (Node node in nodes)
                if (node.id == id)
                    return node;
            return null;
        }

        // 按Id获取线
        public Tie GetTie(int id)
        {
            foreach (Tie tie in ties)
                if (tie.id == id)
                    return tie;
            return null;
        }

        // 获取两个点间的线
        public Tie GetTie(Node node1, Node node2)
        {
            foreach (Tie tie in ties)
                if ((node1.id == tie.node1id && node2.id == tie.node2id) || (node1.id == tie.node2id && node2.id == tie.node1id))
                    return tie;
            return null;
        }

        // 删除点
        public List<Tie> RemoveNode(Node node)
        {
            // 删除这个点本身
            nodes.Remove(node);

            // 删除关于这个点的连接
            List<Tie> needRemove = new List<Tie>();
            foreach (Tie tie in ties)
                if (tie.node1id == node.id || tie.node2id == node.id)
                    needRemove.Add(tie);

            foreach (Tie tie in needRemove)
                RemoveTie(tie);

            return needRemove;
        }

        // 删除连接
        public void RemoveTie(Tie tie)
        {
            ties.Remove(tie);
        }

        public void RemoveTie(Node node1, Node node2)
        {
            Tie tie = GetTie(node1, node2);
            RemoveTie(tie);
        }

        // 加载数据并清空当前的数据
        public void Load(List<Node> nodes, List<Tie> ties)
        {
            this.nodes = nodes;
            this.ties = ties;
        }

        // 获取可序列化的数据
        public MindCanvasFileData GetData()
        {
            MindCanvasFileData inMindFileData = new MindCanvasFileData
            {
                nodes = nodes,
                ties = ties
            };

            return inMindFileData;
        }

        // 清空
        private void RemoveAll()
        {
            nodes.Clear();
            ties.Clear();
        }

        // 修改点
        public Node ModifyNode(int id, string name, string description)
        {
            Node node = GetNode(id);

            node.name = name;
            node.description = description;

            return node;
        }

        public Node ModifyNode(int id, double x, double y)
        {
            Node node = GetNode(id);

            node.x = x;
            node.y = y;

            return node;
        }

        // 修改线
        public Tie ModifyTie(int id, string description)
        {
            Tie tie = GetTie(id);
            tie.description = description;

            return tie;
        }
    }

    // 快照
    public static class Snapshot
    {
        public static IBuffer pixels;
        public static RenderTargetBitmap renderTargetBitmap;


        public static async Task<IBuffer> NewSnapshot(UIElement element)
        {
            //element.UpdateLayout();
            renderTargetBitmap = new RenderTargetBitmap();

            await renderTargetBitmap.RenderAsync(element);
            pixels = await renderTargetBitmap.GetPixelsAsync();

            return pixels;
        }
    }

    // 事件管理器
    // 所有的修改都要通过这个东西
    // 已保存历史操作，提供撤销、重做功能
    public static class EventsManager
    {
        private static List<MindCanvasFileData> records = new List<MindCanvasFileData>();// 记录每次操作后的数据
        private static MindMap mindMap;
        private static MindCanvasFile mindCanvasFile;
        private static MindMapCanvas mindMapCanvas;
        public static int nowIndex;
        public static bool modified = false;

        public static void SetMindMapCanvas(MindMapCanvas newMindMapCanvas)
        {
            mindMapCanvas = newMindMapCanvas;

            mindMapCanvas.SetMindMap(mindMap);
            mindMapCanvas.DrawAll();
        }

        public static void Initialize()
        {
            App.mindMap = mindMap = new MindMap();
            App.mindCanvasFile = mindCanvasFile = new MindCanvasFile();

            mindMap.Initialize();
            mindCanvasFile.SetMindMap(mindMap);
            ClearRecords();
            Record();
            modified = false;
        }


        // 新建文件
        public static async Task<bool> NewFile()
        {
            if (modified)
            {
                ContentDialogResult result = await AskForSave();
                // 用户选择取消
                if (result == ContentDialogResult.None)
                    return false;

                // 用户选择保存
                if (result == ContentDialogResult.Primary)
                {
                    if (await Save())
                    {
                        Initialize();
                        return true;
                    }
                    else
                        return false;// 用户取消了保存
                }
                // 用户选择不保存
                else
                {
                    Initialize();
                    return true;
                }
            }

            // 未修改
            else
            {
                Initialize();
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
            // 当前文件已修改
            if (modified)
            {
                ContentDialogResult result = await AskForSave();

                //用户选择取消
                if (result == ContentDialogResult.None)
                    return false;

                // 用户选择保存
                if (result == ContentDialogResult.Primary)
                {
                    // 要保存好了才能加载
                    if (await Save())
                    {
                        ClearRecords();
                        await mindCanvasFile.LoadFile(file);
                        Record();
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
                    await mindCanvasFile.LoadFile(file);
                    Record();
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
                await mindCanvasFile.LoadFile(file);
                Record();
                modified = false;
                return true;
            }                
        }

        // 保存文件
        public static async Task<bool> Save()
        {
            if (mindCanvasFile.file != null)
            {
                mindCanvasFile.SaveFile();
                modified = false;
                return true;
            }
            else
            {
                var savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                // Dropdown of file types the user can save the file as
                savePicker.FileTypeChoices.Add("MindCanvas 文件", new List<string>() { ".mindcanvas" });
                // Default file name if the user does not type one in or select a file to replace
                savePicker.SuggestedFileName = "无标题";
                StorageFile file = await savePicker.PickSaveFileAsync();

                if (file != null)
                {
                    mindCanvasFile.file = file;
                    mindCanvasFile.SaveFile();
                    return true;
                }
                else
                    return false;
            }
        }

        // 询问是否保存
        public async static Task<ContentDialogResult> AskForSave()
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = "是否希望保存您的思维导图？",
                PrimaryButtonText = "保存",
                SecondaryButtonText = "不保存",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,

            };
            ContentDialogResult result = await dialog.ShowAsync();
            return result;
        }

        // 修改点
        public static void ModifyNode(Node node, string newName, string newDescription)
        {
            Border border = mindMapCanvas.ConvertNodeToBorder(node);
            (border.Child as TextBlock).Text = newName;
            mindMap.ModifyNode(node.id, newName, newDescription);
            Record();
        }

        public static void ModifyNode(Node node, double x, double y)
        {
            Border border = mindMapCanvas.ConvertNodeToBorder(node);
            Canvas.SetTop(border, y);
            Canvas.SetLeft(border, x);
            mindMap.ModifyNode(node.id, x - mindMapCanvas.Width / 2, y - mindMapCanvas.Height / 2);
            Record();
        }

        // 新建点
        public static Node AddNode(string name, string description = "暂无描述")
        {
            Node newNode = mindMap.AddNode(name, description);
            mindMapCanvas.Draw(newNode);
            Record();
            return newNode;
        }

        // 新建线
        public static void AddTie(Node node1, Node node2, string description = "暂无描述")
        {
            Tie newTie = mindMap.AddTie(node1.id, node2.id, description);
            mindMapCanvas.Draw(newTie);
            Record();
        }

        // 删除点
        public static void RemoveNode(Node node)
        {
            mindMap.RemoveNode(node);
            mindMapCanvas.Clear(node);
            Record();
        }

        // 删除线
        public static void RemoveTie(Tie tie)
        {
            mindMap.RemoveTie(tie);
            mindMapCanvas.Clear(tie);
            Record();
        }

        // 撤销
        public static void Undo()
        {
            MindCanvasFileData lastData = records[--nowIndex];
            mindMap.Load(DeepCopy(lastData.nodes), DeepCopy(lastData.ties));
            mindMapCanvas.ReDraw();// 因为mindMapCanvas已与mindMap绑定，所以只需ReDraw刷新即可
            modified = true;
        }

        // 判断能否撤销
        public static bool CanUndo()
        {
            return nowIndex > 0;
        }

        // 重做
        public static void Redo()
        {
            MindCanvasFileData lastData = records[++nowIndex];
            mindMap.Load(DeepCopy(lastData.nodes), DeepCopy(lastData.ties));
            mindMapCanvas.ReDraw();// 因为mindMapCanvas已与mindMap绑定，所以只需ReDraw刷新即可
            modified = true;
        }

        // 判断能否重做
        public static bool CanRedo()
        {
            return records.Count() > nowIndex + 1;
        }

        // 记录一下
        private static void Record()
        {
            // 判断现在是不是在records列表的最后一项操作，是的话直接添加记录就好
            // 如果不是的话，要删除之后的，再添加（类似于新分支）
            // 如果能撤消，说明没在最后一项操作
            if (CanRedo())
                records.RemoveRange(nowIndex + 1, records.Count() - (nowIndex + 1));
            records.Add(DeepCopy(mindMap.GetData()));
            nowIndex++;
            modified = true;
        }

        // 深拷贝
        public static T DeepCopy<T>(T obj)
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
    }

    // 主题切换
    // 来自Xaml-Controls-Gallery
    public static class ThemeHelper
    {
        private const string SelectedAppThemeKey = "SelectedAppTheme";
        private static Window CurrentApplicationWindow;
        // Keep reference so it does not get optimized/garbage collected
        private static UISettings uiSettings;
        /// <summary>
        /// Gets the current actual theme of the app based on the requested theme of the
        /// root element, or if that value is Default, the requested theme of the Application.
        /// </summary>
        public static ElementTheme ActualTheme
        {
            get
            {
                if (Window.Current.Content is FrameworkElement rootElement)
                {
                    if (rootElement.RequestedTheme != ElementTheme.Default)
                    {
                        return rootElement.RequestedTheme;
                    }
                }

                return MindCanvas.App.GetEnum<ElementTheme>(App.Current.RequestedTheme.ToString());
            }
        }

        /// <summary>
        /// Gets or sets (with LocalSettings persistence) the RequestedTheme of the root element.
        /// </summary>
        public static ElementTheme RootTheme
        {
            get
            {
                if (Window.Current.Content is FrameworkElement rootElement)
                {
                    return rootElement.RequestedTheme;
                }

                return ElementTheme.Default;
            }
            set
            {
                if (Window.Current.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = value;
                }

                ApplicationData.Current.LocalSettings.Values[SelectedAppThemeKey] = value.ToString();
                UpdateSystemCaptionButtonColors();
            }
        }

        public static void Initialize()
        {
            // Save reference as this might be null when the user is in another app
            CurrentApplicationWindow = Window.Current;
            string savedTheme = ApplicationData.Current.LocalSettings.Values[SelectedAppThemeKey]?.ToString();

            if (savedTheme != null)
            {
                RootTheme = MindCanvas.App.GetEnum<ElementTheme>(savedTheme);
            }

            // Registering to color changes, thus we notice when user changes theme system wide
            uiSettings = new UISettings();
            uiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;
        }

        private static void UiSettings_ColorValuesChanged(UISettings sender, object args)
        {
            // Make sure we have a reference to our window so we dispatch a UI change
            if (CurrentApplicationWindow != null)
            {
                // Dispatch on UI thread so that we have a current appbar to access and change
                CurrentApplicationWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
                {
                    UpdateSystemCaptionButtonColors();
                });
            }
        }

        public static bool IsDarkTheme()
        {
            if (RootTheme == ElementTheme.Default)
            {
                return Application.Current.RequestedTheme == ApplicationTheme.Dark;
            }
            return RootTheme == ElementTheme.Dark;
        }

        public static void UpdateSystemCaptionButtonColors()
        {
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;

            if (ThemeHelper.IsDarkTheme())
            {
                titleBar.ButtonForegroundColor = Colors.White;
            }
            else
            {
                titleBar.ButtonForegroundColor = Colors.Black;
            }
        }
    }
}
