using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace MindCanvas
{
    // 快照
    public static class Snapshot
    {
        public static IBuffer pixels;
        public static RenderTargetBitmap renderTargetBitmap;


        public static async Task<IBuffer> NewSnapshot(UIElement element)
        {
            //element.UpdateLayout();
            renderTargetBitmap = new RenderTargetBitmap();

            await renderTargetBitmap.RenderAsync(element);
            pixels = await renderTargetBitmap.GetPixelsAsync();

            return pixels;
        }
    }
}
