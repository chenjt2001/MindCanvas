using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Streams;

namespace MindCanvas
{
    // 思维导图文件
    public class MindCanvasFile
    {
        public enum LoadFileResult { Success, VerificationFailed, UnknownError, UserInterrupt };

        // 资源加载器，用于翻译
        private static readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

        private MindMap mindMap;
        private StorageFile file;
        private readonly StorageItemMostRecentlyUsedList mru = StorageApplicationPermissions.MostRecentlyUsedList;
        private bool isEncrypted;
        private string password;

        public StorageFile File { get => file; set => file = value; }
        public MindMap MindMap { get => mindMap; set => mindMap = value; }
        public bool IsEncrypted { get => isEncrypted; }

        // 保存文件
        public async Task SaveFile(bool addToMru = true)
        {
            MindCanvasFileData data = mindMap.GetData();

            using (MemoryStream ms = new MemoryStream())
            {
                object obj = isEncrypted ? EncryptedData.Encrypt(data, password) : data as object;
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                await FileIO.WriteBytesAsync(file, ms.GetBuffer());
            }

            // 将文件添加到MRU（最近访问列表）
            if (addToMru)
                mru.Add(file);
        }

        // 加载文件
        public async Task<LoadFileResult> LoadFile(StorageFile storageFile, bool addToMru = true)
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

                        object obj = formatter.Deserialize(ms);

                        // 判断文件是否被加密
                        MindCanvasFileData data;
                        if (obj is EncryptedData encryptedData)
                        {
                            string[] result = await Dialog.Show.EnterPassword(Dialog.EnterPassword.Mode.RequirePassword);

                            if (result == null)// 取消打开
                                return LoadFileResult.UserInterrupt;

                            try
                            {
                                data = EncryptedData.Decrypt(encryptedData, result[0]);
                            }
                            catch (CryptographicException)
                            {
                                string s = resourceLoader.GetString("Code_FileOpeningFailedPasswordError");// 文件打开失败，密码错误！
                                InfoHelper.ShowInfoBar(s, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                                return LoadFileResult.VerificationFailed;
                            }

                            password = result[0];
                            isEncrypted = true;
                        }

                        else
                        {
                            data = (MindCanvasFileData)obj;
                            isEncrypted = false;
                        }

                        MindCanvasFileData.VersionHelper(ref data);

                        mindMap.LoadData(data);

                        if (addToMru)
                            mru.Add(storageFile);
                    }
                }

                file = storageFile;
                return LoadFileResult.Success;
            }
            catch (Exception)
            {
                await Dialog.Show.OpenFileError();
                return LoadFileResult.UnknownError;
            }
        }

        // 设置密码
        public async Task<bool> SetPassword(string oldPassword, string newPassword)
        {
            LogHelper.Info("SetPassword");

            if (oldPassword == this.password)
            {
                await LoadingHelper.ShowLoading();

                this.password = newPassword;
                if (newPassword != null)
                    isEncrypted = true;
                else
                    isEncrypted = false;

                if (file != null)
                    await SaveFile();

                LoadingHelper.HideLoading();

                return true;
            }

            else
                return false;
        }
    }
}
