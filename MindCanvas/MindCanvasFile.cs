using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Streams;

namespace MindCanvas
{
    // 思维导图文件
    public class MindCanvasFile
    {
        private MindMap mindMap;
        private StorageFile file;
        private readonly StorageItemMostRecentlyUsedList mru = StorageApplicationPermissions.MostRecentlyUsedList;

        public StorageFile File { get => file; set => file = value; }
        public MindMap MindMap { get => mindMap; set => mindMap = value; }

        // 保存文件
        public async Task SaveFile(bool addToMru = true)
        {
            MindCanvasFileData data = mindMap.GetData();

            using (MemoryStream ms = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, data);
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
                        MindCanvasFileData data = (MindCanvasFileData)formatter.Deserialize(ms);
                        MindCanvasFileData.VersionHelper(ref data);
                        MindCanvasFileData.VersionHelper(ref data);

                        mindMap.Load(data);
                        if (addToMru)
                            mru.Add(storageFile);
                    }
                }

                file = storageFile;
                return true;
            }
            catch (Exception)
            {
                await Dialog.Show.OpenFileError();
                return false;
            }
        }
    }
}
