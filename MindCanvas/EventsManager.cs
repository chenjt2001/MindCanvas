﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

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
        public static bool modified = false;
        public static int nowIndex;


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
                ContentDialogResult result = await Dialog.AskForSave();
                // 用户选择取消
                if (result == ContentDialogResult.None)
                    return false;

                // 用户选择保存
                if (result == ContentDialogResult.Primary)
                {
                    if (await Save())
                    {
                        Initialize();
                        ResetMainPageCache();
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
                    return true;
                }
            }

            // 未修改
            else
            {
                Initialize();
                ResetMainPageCache();
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
                ContentDialogResult result = await Dialog.AskForSave();

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
                        ResetMainPageCache();
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
                    ResetMainPageCache();
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
                ResetMainPageCache();
                modified = false;
                return true;
            }
        }

        // 清除主页面缓存
        public static void ResetMainPageCache()
        {
            if (MainPage.mainPage != null)
                MainPage.mainPage.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Disabled;
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

        // 修改点
        public static void ModifyNode(Node node, string newName, string newDescription)
        {
            NodeControl border = mindMapCanvas.ConvertNodeToBorder(node);

            border.Text = newName;
            mindMapCanvas.ReDrawTies(node);

            mindMap.ModifyNode(node.Id, newName, newDescription);
            Record();
        }

        public static void ModifyNode(Node node, double x, double y)
        {
            mindMap.ModifyNode(node.Id, x - mindMapCanvas.Width / 2, y - mindMapCanvas.Height / 2);

            NodeControl border = mindMapCanvas.ConvertNodeToBorder(node);
            Canvas.SetTop(border, y);
            Canvas.SetLeft(border, x);
            mindMapCanvas.ReDrawTies(node);

            Record();
        }

        public static void ModifyNodeBorderBrushColor(Node node, Color? borderBrushColor)
        {
            NodeControl border = mindMapCanvas.ConvertNodeToBorder(node);
            if (borderBrushColor != null)
            {
                SolidColorBrush borderBrush = new SolidColorBrush((Color)borderBrushColor);
                border.BorderBrush = borderBrush;
                node.BorderBrush = borderBrush;
            }
            else
            {
                node.BorderBrush = null;
                border.BorderBrush = mindMap.defaultNodeBorderBrush;
            }

            Record();
        }

        public static void ModifyNodeNameFontSize(Node node, double? nameFontSize)
        {
            NodeControl border = mindMapCanvas.ConvertNodeToBorder(node);
            if (nameFontSize != null)
            {
                node.NameFontSize = (double)nameFontSize;
                border.FontSize = (double)nameFontSize;
            }
            else
            {
                node.NameFontSize = 0.0d;
                border.FontSize = mindMap.defaultNodeNameFontSize;
            }

            mindMapCanvas.ReDrawTies(node);

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
            Tie newTie = mindMap.AddTie(node1.Id, node2.Id, description);
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
            MindCanvasFileData lastData = records[++nowIndex];
            mindMap.Load(DeepCopy(lastData.nodes), DeepCopy(lastData.ties));
            mindMapCanvas.ReDraw();// 因为mindMapCanvas已与mindMap绑定，所以只需ReDraw刷新即可
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
}