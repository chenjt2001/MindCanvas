using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace MindCanvas
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class ExportPage : Page
    {
        private MindMapCanvas mindMapCanvas;
        private MindMapInkCanvas mindMapInkCanvas;

        // 资源加载器，用于翻译
        private static readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

        public ExportPage()
        {
            this.InitializeComponent();
        }

        private async void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
            // 获取选择的项
            string choice = (string)FormatComboBox.SelectedItem;

            if (choice == null)
            {
                //FormatTip.IsOpen = true;
                await Dialog.Show.ExportError();
            }
            else
            {
                var savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

                // 文件类型
                switch (choice)
                {
                    case "JPEG (*.jpg)":
                        savePicker.FileTypeChoices.Add("JPEG", new List<string>() { ".jpg" });
                        break;

                    case "PNG (*.png)":
                        savePicker.FileTypeChoices.Add("PNG", new List<string>() { ".png" });
                        break;

                    case "HEIC (*.heic)":
                        savePicker.FileTypeChoices.Add("HEIC", new List<string>() { ".heic" });
                        break;
                }

                // 默认文件名称
                if (App.mindCanvasFile.File == null)
                    savePicker.SuggestedFileName = resourceLoader.GetString("Code_MindCanvasMindMap");//MindCanvas 思维导图
                else
                    savePicker.SuggestedFileName = App.mindCanvasFile.File.DisplayName;

                StorageFile file = await savePicker.PickSaveFileAsync();

                if (file != null)
                {
                    try
                    {
                        switch (choice)
                        {
                            case "JPEG (*.jpg)":
                                await Export(file, BitmapEncoder.JpegEncoderId);
                                break;

                            case "PNG (*.png)":
                                await Export(file, BitmapEncoder.PngEncoderId);
                                break;

                            case "HEIC (*.heic)":
                                await Export(file, BitmapEncoder.HeifEncoderId);
                                break;
                        }
                    }
                    catch
                    {
                        await Dialog.Show.ExportError();
                    }
                }
            }
        }

        // 导出
        private async Task Export(StorageFile file, Guid format)
        {
            LogHelper.Info("Export");

            // 合成准备
            Rect inkBoundingRect = mindMapInkCanvas.InkPresenter.StrokeContainer.BoundingRect;
            Rect canvasBoundingRect = mindMapCanvas.BoundingRect;

            bool hasInk = inkBoundingRect != new Rect();
            bool hasCanvas = canvasBoundingRect != new Rect();

            Color background = Colors.Transparent;
            if (format == BitmapEncoder.JpegEncoderId || format == BitmapEncoder.HeifEncoderId)
            {
                background = Colors.White;
            }

            WriteableBitmap mindMapBitmap;

            // 生成Ink图像
            byte[] inkPixelData = null;
            int inkWidth = 0, inkHeight = 0;
            if (hasInk)
            {
                BitmapDecoder decoder;
                using (InMemoryRandomAccessStream ms = new InMemoryRandomAccessStream())
                {
                    // 因为BitmapDecoder解gif好像无法处理alpha通道
                    // 所以先转png，再用BitmapDecoder解
                    await App.mindMap.InkStrokeContainer.SaveAsync(ms.AsStreamForWrite().AsOutputStream(), Windows.UI.Input.Inking.InkPersistenceFormat.GifWithEmbeddedIsf);
                    decoder = await BitmapDecoder.CreateAsync(BitmapDecoder.GifDecoderId, ms);

                    SoftwareBitmap inkSoftwareBitmap = await decoder.GetSoftwareBitmapAsync();
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, ms);
                    encoder.SetSoftwareBitmap(inkSoftwareBitmap);
                    await encoder.FlushAsync();

                    decoder = await BitmapDecoder.CreateAsync(BitmapDecoder.PngDecoderId, ms);
                }
                PixelDataProvider provider = await decoder.GetPixelDataAsync();
                inkPixelData = provider.DetachPixelData();
                inkWidth = (int)decoder.PixelWidth;
                inkHeight = (int)decoder.PixelHeight;
            }

            // 生成Canvas图像
            byte[] canvasPixelData = null;
            int canvasWidth = 0, canvasHeight = 0;
            if (hasCanvas)
            {
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
                await renderTargetBitmap.RenderAsync(mindMapCanvas);
                canvasPixelData = (await renderTargetBitmap.GetPixelsAsync()).ToArray();
                canvasWidth = renderTargetBitmap.PixelWidth;
                canvasHeight = renderTargetBitmap.PixelHeight;

                // 因为获取到的是高Dpi缩放过的，所以要还原缩放
                using (InMemoryRandomAccessStream ms = new InMemoryRandomAccessStream())
                {
                    DisplayInformation displayInformation = DisplayInformation.GetForCurrentView();
                    int scaledWidth = (int)(canvasWidth / displayInformation.RawPixelsPerViewPixel);
                    int scaledHeight = (int)(canvasHeight / displayInformation.RawPixelsPerViewPixel);

                    SoftwareBitmap canvasSoftwareBitmap = SoftwareBitmap.CreateCopyFromBuffer(canvasPixelData.AsBuffer(), BitmapPixelFormat.Bgra8, canvasWidth, canvasHeight);
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, ms);
                    encoder.SetSoftwareBitmap(canvasSoftwareBitmap);
                    await encoder.FlushAsync();

                    var decoder = await BitmapDecoder.CreateAsync(BitmapDecoder.PngDecoderId, ms);
                    var transform = new BitmapTransform() { ScaledWidth = (uint)scaledWidth, ScaledHeight = (uint)scaledHeight, InterpolationMode = BitmapInterpolationMode.Cubic };
                    var pixelData = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, transform, ExifOrientationMode.RespectExifOrientation, ColorManagementMode.ColorManageToSRgb);

                    canvasPixelData = pixelData.DetachPixelData();
                    canvasWidth = scaledWidth;
                    canvasHeight = scaledHeight;
                }
            }

            // 合成
            // 只有ink
            if (hasInk && !hasCanvas)
            {
                mindMapBitmap = new WriteableBitmap(inkWidth, inkHeight);
                using (Stream stream = mindMapBitmap.PixelBuffer.AsStream())
                {
                    byte[] byteArray = new byte[stream.Length];
                    await stream.ReadAsync(byteArray, 0, byteArray.Length);
                    for (int i = 0; i < byteArray.Length; i += 4)
                    {
                        byteArray[i] = inkPixelData[i];
                        byteArray[i + 1] = inkPixelData[i + 1];
                        byteArray[i + 2] = inkPixelData[i + 2];
                        byteArray[i + 3] = inkPixelData[i + 3];

                        // 写入背景色
                        Color color = new Color()
                        {
                            B = byteArray[i],
                            G = byteArray[i + 1],
                            R = byteArray[i + 2],
                            A = byteArray[i + 3]
                        };
                        color = BlendColor(background, color, backgroundMode: true);
                        byteArray[i] = color.B;
                        byteArray[i + 1] = color.G;
                        byteArray[i + 2] = color.R;
                        byteArray[i + 3] = color.A;
                    }
                    stream.Position = 0;
                    await stream.WriteAsync(byteArray, 0, byteArray.Length);
                }
            }

            // 只有canvas
            else if (hasCanvas && !hasInk)
            {
                mindMapBitmap = new WriteableBitmap(canvasWidth, canvasHeight);
                using (Stream stream = mindMapBitmap.PixelBuffer.AsStream())
                {
                    byte[] byteArray = new byte[stream.Length];
                    await stream.ReadAsync(byteArray, 0, byteArray.Length);
                    for (int i = 0; i < byteArray.Length; i += 4)
                    {
                        byteArray[i] = canvasPixelData[i];
                        byteArray[i + 1] = canvasPixelData[i + 1];
                        byteArray[i + 2] = canvasPixelData[i + 2];
                        byteArray[i + 3] = canvasPixelData[i + 3];

                        // 写入背景色
                        Color color = new Color()
                        {
                            B = byteArray[i],
                            G = byteArray[i + 1],
                            R = byteArray[i + 2],
                            A = byteArray[i + 3]
                        };
                        color = BlendColor(background, color, backgroundMode: true);
                        byteArray[i] = color.B;
                        byteArray[i + 1] = color.G;
                        byteArray[i + 2] = color.R;
                        byteArray[i + 3] = color.A;
                    }
                    stream.Position = 0;
                    await stream.WriteAsync(byteArray, 0, byteArray.Length);
                }
            }

            // 都有
            else
            {
                int inkLeft = (int)inkBoundingRect.Left;
                int inkTop = (int)inkBoundingRect.Top;
                int canvasLeft = (int)canvasBoundingRect.Left;
                int canvasTop = (int)canvasBoundingRect.Top;

                int left = inkLeft < canvasLeft ? inkLeft : canvasLeft;
                int top = inkTop < canvasTop ? inkTop : canvasTop;

                inkLeft -= left;
                inkTop -= top;
                canvasLeft -= left;
                canvasTop -= top;

                int width = inkLeft + inkWidth > canvasLeft + canvasWidth ? inkLeft + inkWidth : canvasLeft + canvasWidth;
                int height = inkTop + inkHeight > canvasTop + canvasHeight ? inkTop + inkHeight : canvasTop + canvasHeight;

                mindMapBitmap = new WriteableBitmap(width, height);

                // 合成
                using (Stream stream = mindMapBitmap.PixelBuffer.AsStream())
                {
                    byte[] byteArray = new byte[stream.Length];
                    await stream.ReadAsync(byteArray, 0, byteArray.Length);

                    for (int h = 0; h < height; h++)
                    {
                        for (int w = 0; w < width; w++)
                        {
                            int i = h * width * 4 + w * 4;

                            // 写入canvas
                            if (w >= canvasLeft &&
                                w < canvasLeft + canvasWidth &&
                                h >= canvasTop &&
                                h < canvasTop + canvasHeight)
                            {
                                byteArray[i] = canvasPixelData[(w - canvasLeft) * 4 + (h - canvasTop) * canvasWidth * 4];
                                byteArray[i + 1] = canvasPixelData[(w - canvasLeft) * 4 + (h - canvasTop) * canvasWidth * 4 + 1];
                                byteArray[i + 2] = canvasPixelData[(w - canvasLeft) * 4 + (h - canvasTop) * canvasWidth * 4 + 2];
                                byteArray[i + 3] = canvasPixelData[(w - canvasLeft) * 4 + (h - canvasTop) * canvasWidth * 4 + 3];
                            }

                            // 写入ink
                            if (w >= inkLeft &&
                                w < inkLeft + inkWidth &&
                                h >= inkTop &&
                                h < inkTop + inkHeight)
                            {
                                Color oldColor = new Color()
                                {
                                    B = byteArray[i],
                                    G = byteArray[i + 1],
                                    R = byteArray[i + 2],
                                    A = byteArray[i + 3]
                                };

                                Color inkColor = new Color()
                                {
                                    B = inkPixelData[(w - inkLeft) * 4 + (h - inkTop) * inkWidth * 4],
                                    G = inkPixelData[(w - inkLeft) * 4 + (h - inkTop) * inkWidth * 4 + 1],
                                    R = inkPixelData[(w - inkLeft) * 4 + (h - inkTop) * inkWidth * 4 + 2],
                                    A = inkPixelData[(w - inkLeft) * 4 + (h - inkTop) * inkWidth * 4 + 3]
                                };
                                Color newColor = BlendColor(oldColor, inkColor);
                                byteArray[i] = newColor.B;
                                byteArray[i + 1] = newColor.G;
                                byteArray[i + 2] = newColor.R;
                                byteArray[i + 3] = newColor.A;
                            }

                            // 写入背景色
                            Color color = new Color()
                            {
                                B = byteArray[i],
                                G = byteArray[i + 1],
                                R = byteArray[i + 2],
                                A = byteArray[i + 3]
                            };
                            color = BlendColor(background, color, backgroundMode: true);
                            byteArray[i] = color.B;
                            byteArray[i + 1] = color.G;
                            byteArray[i + 2] = color.R;
                            byteArray[i + 3] = color.A;
                        }
                    }
                    stream.Position = 0;
                    await stream.WriteAsync(byteArray, 0, byteArray.Length);
                }
            }

            // 输出到文件
            SoftwareBitmap softwareBitmap = SoftwareBitmap.CreateCopyFromBuffer(mindMapBitmap.PixelBuffer, BitmapPixelFormat.Bgra8, mindMapBitmap.PixelWidth, mindMapBitmap.PixelHeight);

            using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(format, fileStream);
                encoder.SetSoftwareBitmap(softwareBitmap);
                await encoder.FlushAsync();
            }
        }

        // 缩放
        private static SoftwareBitmap Resize(SoftwareBitmap softwareBitmap, float newWidth, float newHeight)
        {
            using (var resourceCreator = CanvasDevice.GetSharedDevice())
            using (var canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(resourceCreator, softwareBitmap))
            using (var canvasRenderTarget = new CanvasRenderTarget(resourceCreator, newWidth, newHeight, canvasBitmap.Dpi))
            using (var drawingSession = canvasRenderTarget.CreateDrawingSession())
            using (var scaleEffect = new ScaleEffect())
            {
                scaleEffect.Source = canvasBitmap;
                scaleEffect.Scale = new Vector2(newWidth / softwareBitmap.PixelWidth, newHeight / softwareBitmap.PixelHeight);
                drawingSession.DrawImage(scaleEffect);
                drawingSession.Flush();
                return SoftwareBitmap.CreateCopyFromBuffer(canvasRenderTarget.GetPixelBytes().AsBuffer(), BitmapPixelFormat.Bgra8, (int)newWidth, (int)newHeight);
            }
        }

        // 混合颜色
        private static Color BlendColor(Color color1, Color color2, bool backgroundMode = false)
        {
            double a1 = Convert.ToInt32(color1.A) / 255.0;
            double a2 = Convert.ToInt32(color2.A) / 255.0;
            int r1 = Convert.ToInt32(color1.R);
            int r2 = Convert.ToInt32(color2.R);

            int g1 = Convert.ToInt32(color1.G);
            int g2 = Convert.ToInt32(color2.G);

            int b1 = Convert.ToInt32(color1.B);
            int b2 = Convert.ToInt32(color2.B);

            int a = (int)((a1 + a2 - a1 * a2) * 255);

            int r;
            int g;
            int b;
            if (backgroundMode)
            {
                r = (int)(r1 * (1 - a2) + r2 * a2);
                g = (int)(g1 * (1 - a2) + g2 * a2);
                b = (int)(b1 * (1 - a2) + b2 * a2);
            }
            else
            {
                if (a1 == 0)
                {
                    r = r2;
                    g = g2;
                    b = b2;
                }
                else if (a2 == 0)
                {
                    r = r1;
                    g = g1;
                    b = b1;
                }
                else
                {
                    r = (r1 + r2) / 2;
                    g = (g1 + g2) / 2;
                    b = (b1 + b2) / 2;
                }
            }

            return new Color()
            {
                A = Convert.ToByte(a),
                R = Convert.ToByte(r),
                G = Convert.ToByte(g),
                B = Convert.ToByte(b),
            };
        }

        private void MindMapBorder_Loaded(object sender, RoutedEventArgs e)
        {
            Canvas canvas = new Canvas();
            MindMapBorder.Child = canvas;

            mindMapCanvas = new MindMapCanvas(showAnimation: false);
            mindMapCanvas.MindMap = App.mindMap;
            canvas.Children.Add(mindMapCanvas);
            mindMapCanvas.DrawAll();

            mindMapInkCanvas = new MindMapInkCanvas();
            mindMapInkCanvas.InkPresenter.StrokeContainer = App.mindMap.InkStrokeContainer;
            canvas.Children.Add(mindMapInkCanvas);

            Rect boundingRect1 = mindMapInkCanvas.InkPresenter.StrokeContainer.BoundingRect;
            Rect boundingRect2 = mindMapCanvas.BoundingRect;

            bool hasInk = boundingRect1 != new Rect();
            bool hasCanvas = boundingRect2 != new Rect();

            double width;
            double height;
            double left;
            double top;
            if (hasInk && !hasCanvas)
            {
                width = boundingRect1.Width;
                height = boundingRect1.Height;
                left = boundingRect1.Left;
                top = boundingRect1.Top;
            }
            else if (hasCanvas && !hasInk)
            {
                width = boundingRect2.Width;
                height = boundingRect2.Height;
                left = boundingRect2.Left;
                top = boundingRect2.Top;
            }
            else if (hasCanvas && hasInk)
            {
                width = boundingRect1.Width > boundingRect2.Width ? boundingRect1.Width : boundingRect2.Width;
                height = boundingRect1.Height > boundingRect2.Height ? boundingRect1.Height : boundingRect2.Height;
                left = boundingRect1.Left < boundingRect2.Left ? boundingRect1.Left : boundingRect2.Left;
                top = boundingRect1.Top < boundingRect2.Top ? boundingRect1.Top : boundingRect2.Top;
            }
            else
            {
                ExportBtn.IsEnabled = false;
                FormatComboBox.IsEnabled = false;
                return;
            }

            canvas.Width = width;
            canvas.Height = height;

            Canvas.SetLeft(mindMapInkCanvas, -left);
            Canvas.SetTop(mindMapInkCanvas, -top);
            Canvas.SetLeft(mindMapCanvas, -left);
            Canvas.SetTop(mindMapCanvas, -top);

            MindMapBorder.Width = width;
            MindMapBorder.Height = height;

            // 防止MindMapViewbox自动缩放预览大小
            MindMapViewbox.UpdateLayout();
            if (width < MindMapViewbox.ActualWidth)
                MindMapViewbox.MaxWidth = width;
        }
    }
}
