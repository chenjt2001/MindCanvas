using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using Windows.ApplicationModel.Resources;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Media;

namespace MindCanvas
{

    [Serializable]// 标记为可序列化
    public class Node
    {
        private int id;
        private string description;

        // 兼容多版本
        [OptionalField]
        private string name;
        [OptionalField]
        private string title;
        [OptionalField]
        private byte[] borderBrushArgb;// 边框颜色
        [OptionalField]
        private double? nameFontSize;// 名称字体大小
        [OptionalField]
        private int? parentNodeId;// 父节点
        [OptionalField]
        private string style;// 样式

        // 相对于画布中心的位置
        private double x;
        private double y;

        // 资源加载器，用于翻译
        private static readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

        // 边框颜色
        public Brush BorderBrush
        {
            get
            {
                if (borderBrushArgb == null)// 默认
                    return null;
                else// 已设置
                    return new SolidColorBrush(Color.FromArgb(borderBrushArgb[0], borderBrushArgb[1], borderBrushArgb[2], borderBrushArgb[3]));
            }

            set
            {
                if (value == null)
                    borderBrushArgb = null;
                else
                {
                    Color color = (value as SolidColorBrush).Color;
                    borderBrushArgb = new byte[4] { color.A, color.R, color.G, color.B };
                }
            }
        }

        // 版本兼容
        public static void VersionHelper(Node node)
        {
            // V1.0 -> V1.1
            if (node.title != null && node.name == null)
                node.name = node.title;

            // V1.3 -> V1.4
            if (node.nameFontSize == 0.0d)
                node.nameFontSize = null;

            // V1.5 -> V1.6
            if (node.description == resourceLoader.GetString("Code_NoDescription"))// 暂无描述
                node.description = "";
        }

        public int Id { get => id; set => id = value; }
        public string Description { get => description; set => description = value; }
        public string Name { get => name; set => name = value; }
        public byte[] BorderBrushArgb { get => borderBrushArgb; set => borderBrushArgb = value; }
        public double X { get => x; set => x = value; }
        public double Y { get => y; set => y = value; }
        public double? NameFontSize { get => nameFontSize; set => nameFontSize = value; }
        public int? ParentNodeId { get => parentNodeId; set => parentNodeId = value; }
        public string Style { get => style; set => style = value; }
    }

    [Serializable]
    public class Tie
    {
        private int id;
        private string description;
        private int node1id;
        private int node2id;

        // 资源加载器，用于翻译
        private static readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

        // 版本兼容
        public static void VersionHelper(Tie tie)
        {
            // V1.5 -> V1.6
            if (tie.description == resourceLoader.GetString("Code_NoDescription"))// 暂无描述
                tie.description = "";
        }

        public int Id { get => id; set => id = value; }
        public string Description { get => description; set => description = value; }
        public int Node1Id { get => node1id; set => node1id = value; }
        public int Node2Id { get => node2id; set => node2id = value; }
    }

    [Serializable]
    public struct MindCanvasFileData
    {
        private List<Node> nodes;
        private List<Tie> ties;
        [OptionalField]
        private byte[] defaultNodeBorderBrushArgb;// 默认边框颜色
        [OptionalField]
        private double? defaultNodeNameFontSize;// 默认点名称字体大小
        [OptionalField]
        private byte[] inkStrokeContainerData;// 墨迹数据
        [OptionalField]
        private double? visualCenterX;// 可视中心点X
        [OptionalField]
        private double? visualCenterY;// 可视中心点Y
        [OptionalField]
        private float? zoomFactor;// 可视区放大倍数
        [OptionalField]
        private string defaultNodeStyle;// 默认点样式

        public Brush DefaultNodeBorderBrush
        {
            get
            {
                return new SolidColorBrush(Color.FromArgb(
                    defaultNodeBorderBrushArgb[0],
                    defaultNodeBorderBrushArgb[1],
                    defaultNodeBorderBrushArgb[2],
                    defaultNodeBorderBrushArgb[3]));
            }
            set
            {
                Color color = (value as SolidColorBrush).Color;
                defaultNodeBorderBrushArgb = new byte[4] { color.A, color.R, color.G, color.B };
            }
        }

        public InkStrokeContainer InkStrokeContainer
        {
            get
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(inkStrokeContainerData, 0, inkStrokeContainerData.Length);
                    ms.Position = 0;

                    InkStrokeContainer inkStrokeContainer = new InkStrokeContainer();
                    inkStrokeContainer.LoadAsync(ms.AsInputStream()).GetResults();
                    return inkStrokeContainer;
                }
            }
            set
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    if (value == null)
                        value = new InkStrokeContainer();
                    value.SaveAsync(ms.AsOutputStream(), inkPersistenceFormat: InkPersistenceFormat.Isf).GetResults();
                    inkStrokeContainerData = ms.ToArray();
                }
            }
        }

        // 版本兼容
        public static void VersionHelper(ref MindCanvasFileData data)
        {
            foreach (Node node in data.Nodes)
                Node.VersionHelper(node);
            foreach (Tie tie in data.Ties)
                Tie.VersionHelper(tie);

            // V1.1 -> V1.2
            if (data.defaultNodeBorderBrushArgb == null)
                data.defaultNodeBorderBrushArgb = new byte[4] { InitialValues.NodeBorderBrushColor.A, InitialValues.NodeBorderBrushColor.R, InitialValues.NodeBorderBrushColor.G, InitialValues.NodeBorderBrushColor.B };
            if (data.defaultNodeNameFontSize == null)
                data.defaultNodeNameFontSize = InitialValues.NodeNameFontSize;

            // V1.2 -> V1.3
            if (data.inkStrokeContainerData == null)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    new InkStrokeContainer().SaveAsync(ms.AsOutputStream(), inkPersistenceFormat: InkPersistenceFormat.Isf).GetResults();
                    data.inkStrokeContainerData = ms.ToArray();
                }
            }

            // V1.3 -> V1.4
            if (data.visualCenterX == null)
                if (data.Nodes.Count() > 0)
                    data.visualCenterX = data.Nodes[0].X;
                else
                    data.visualCenterX = 0;

            if (data.visualCenterY == null)
                if (data.Nodes.Count() > 0)
                    data.visualCenterY = data.Nodes[0].Y;
                else
                    data.visualCenterY = 0;

            if (data.zoomFactor == null)
                data.zoomFactor = 1;

            // V1.5 -> V1.6
            if (data.defaultNodeStyle == null)
                data.defaultNodeStyle = InitialValues.NodeStyle;
        }

        public List<Node> Nodes { get => nodes; set => nodes = value; }
        public List<Tie> Ties { get => ties; set => ties = value; }
        public double DefaultNodeNameFontSize { get => defaultNodeNameFontSize.Value; set => defaultNodeNameFontSize = value; }
        public float ZoomFactor { get => zoomFactor.Value; set => zoomFactor = value; }
        public double VisualCenterX { get => visualCenterX.Value; set => visualCenterX = value; }
        public double VisualCenterY { get => visualCenterY.Value; set => visualCenterY = value; }
        public string DefaultNodeStyle { get => defaultNodeStyle; set => defaultNodeStyle = value; }
    }

    // 加密的数据
    [Serializable]
    public struct EncryptedData
    {
        public string Tag => "MindCanvasFile";
        public byte[] Data { get; set; }

        // 加密
        public static EncryptedData Encrypt(MindCanvasFileData data, string password)
        {
            // password转md5
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] passwordMd5 = md5.ComputeHash(Encoding.UTF8.GetBytes(password));// 作为aes的key

            // 序列化
            byte[] original;
            using (MemoryStream ms = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, data);
                original = ms.ToArray();
            }

            // AES加密
            RijndaelManaged rDel = new RijndaelManaged();
            rDel.Key = passwordMd5;
            rDel.Mode = CipherMode.ECB;
            rDel.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = rDel.CreateEncryptor();

            byte[] result = cTransform.TransformFinalBlock(original, 0, original.Length);

            // 创建EncryptedData
            EncryptedData encryptedData = new EncryptedData();
            encryptedData.Data = result;

            return encryptedData;
        }

        // 解密
        public static MindCanvasFileData Decrypt(EncryptedData encryptedData, string password)
        {
            // password转md5
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] passwordMd5 = md5.ComputeHash(Encoding.UTF8.GetBytes(password));

            // AES解密
            RijndaelManaged rDel = new RijndaelManaged();
            rDel.Key = passwordMd5;
            rDel.Mode = CipherMode.ECB;
            rDel.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = rDel.CreateDecryptor();

            byte[] result = cTransform.TransformFinalBlock(encryptedData.Data, 0, encryptedData.Data.Length);

            // 反序列化
            object resultObject;
            using (MemoryStream ms = new MemoryStream(result))
            {
                // 反序列化
                IFormatter formatter = new BinaryFormatter();
                resultObject = formatter.Deserialize(ms);
            }

            return (MindCanvasFileData)resultObject;
        }
    }

    public class Item
    {
        public Item(string text, object tag = null)
        {
            this.Text = text;
            this.Tag = tag;
        }

        public override string ToString() => Text;

        public object Tag { get; set; }
        public string Text { get; set; }
    }
}
