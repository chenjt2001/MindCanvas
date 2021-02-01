using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace MindCanvas
{
    public class MindMapInkCanvas: InkCanvas
    {
        private InkToolbar inkToolbar;

        public MindMapInkCanvas()
        {
            //InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;

            // 画布大小
            Width = 100000;
            Height = 100000;
        }

        public void SetInkToolbar(InkToolbar inkToolbar)
        {
            this.inkToolbar = inkToolbar;
        }
    }
}
