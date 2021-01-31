using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml.Controls;

namespace MindCanvas
{
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
        public async Task<bool> LoadFile(StorageFile storageFile, bool addToMru = true)
        {
            try
            {
                IBuffer buffer = await FileIO.ReadBufferAsync(storageFile);

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
                            mru.Add(storageFile);
                    }
                }

                file = storageFile;
                return true;
            }
            catch (Exception)
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = "错误",
                    Content = "文件打开失败！请确认此文件为 MindCanvas 格式的文件并尝试将 MindCanvas 更新至最新版本！",
                    CloseButtonText = "好的"
                };
                await dialog.ShowAsync();
                return false;
            }
        }

        // 兼容版本
        private void VersionHelper(ref MindCanvasFileData data)
        {
            // V1.0 -> V1.1
            foreach (Node node in data.nodes)
                if (node.title != null && node.Name == null)
                    node.Name = node.title;

            // V1.1 -> V1.2
            if (data.defaultNodeBorderBrushArgb == null)
                data.defaultNodeBorderBrushArgb = new byte[4] { Colors.Blue.A, Colors.Blue.R, Colors.Blue.G, Colors.Blue.B };
            if (data.defaultNodeNameFontSize == 0.0d)
                data.defaultNodeNameFontSize = 20;
        }
    }
}
