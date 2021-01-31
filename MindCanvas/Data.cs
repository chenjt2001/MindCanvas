using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI;
using Windows.UI.Xaml.Media;
using System.Runtime.Serialization;

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
        public string title;
        [OptionalField]
        private byte[] borderBrushArgb;// 边框颜色
        [OptionalField]
        private double nameFontSize;// 名称字体大小

        // 相对于画布中心的位置
        private double x;
        private double y;

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

        public int Id { get => id; set => id = value; }
        public string Description { get => description; set => description = value; }
        public string Name { get => name; set => name = value; }
        public byte[] BorderBrushArgb { get => borderBrushArgb; set => borderBrushArgb = value; }
        public double X { get => x; set => x = value; }
        public double Y { get => y; set => y = value; }
        public double NameFontSize { get => nameFontSize; set => nameFontSize = value; }
    };

    [Serializable]
    public class Tie
    {
        private int id;
        private string description;
        private int node1id;
        private int node2id;

        public int Id { get => id; set => id = value; }
        public string Description { get => description; set => description = value; }
        public int Node1Id { get => node1id; set => node1id = value; }
        public int Node2Id { get => node2id; set => node2id = value; }
    }

    [Serializable]
    public struct MindCanvasFileData
    {
        public List<Node> nodes;
        public List<Tie> ties;
        [OptionalField]
        public byte[] defaultNodeBorderBrushArgb;// 默认边框颜色
        [OptionalField]
        public double defaultNodeNameFontSize;// 默认点名称字体大小
    }
}
