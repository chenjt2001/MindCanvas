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
        private static IBuffer pixels;
        private static RenderTargetBitmap renderTargetBitmap;

        public static IBuffer Pixels { get => pixels; set => pixels = value; }
        public static RenderTargetBitmap RenderTargetBitmap { get => renderTargetBitmap; set => renderTargetBitmap = value; }

        public static async Task<IBuffer> NewSnapshot(UIElement element)
        {
            //element.UpdateLayout();
            RenderTargetBitmap = new RenderTargetBitmap();

            await RenderTargetBitmap.RenderAsync(element);
            Pixels = await RenderTargetBitmap.GetPixelsAsync();

            return Pixels;
        }
    }
}
