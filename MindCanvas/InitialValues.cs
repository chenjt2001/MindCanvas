using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace MindCanvas
{
    // 定义一些初始值
    public static class InitialValues
    {
        public static readonly Color NodeBorderBrushColor = Colors.Blue;
        public const int NodeNameFontSize = 20;
        public const float MinZoomFactor = 0.4f;
        public const float MaxZoomFactor = 2;
    }
}
