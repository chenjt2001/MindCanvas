using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Xaml.Controls;

namespace MindCanvas
{
    public class MindCanvasGrid: Grid
    {
        public MindMapCanvas mindMapCanvas;
        public InkCanvas inkCanvas;

        public MindCanvasGrid(MindMapCanvas mindMapCanvas, InkCanvas inkCanvas)
        {
            this.mindMapCanvas = mindMapCanvas;
            this.inkCanvas = inkCanvas;
        }
    }
}
