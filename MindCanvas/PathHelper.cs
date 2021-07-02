using Windows.UI;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace MindCanvas
{
    static class PathHelper
    {
        public static Windows.UI.Xaml.Shapes.Path NewPath(double x1, double y1, double x2, double y2)
        {
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
            string commands = string.Format("M {0} {1} C {4} {1} {4} {3} {2} {3}", x1, y1, x2, y2, (x1 + x2) / 2);
            Geometry data = XamlBindingHelper.ConvertValue(typeof(Geometry), commands) as Geometry;
            path.Data = data;

            return path;
        }

        public static void ModifyPath(Windows.UI.Xaml.Shapes.Path path, double x1, double y1, double x2, double y2)
        {
            string commands = string.Format("M {0} {1} C {4} {1} {4} {3} {2} {3}", x1, y1, x2, y2, (x1 + x2) / 2);
            Geometry data = XamlBindingHelper.ConvertValue(typeof(Geometry), commands) as Geometry;
            path.Data = data;
        }
    }
}
