using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.Storage.Pickers;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Storage;
using Windows.Graphics.Imaging;
using Windows.UI;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace MindCanvas
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class OutputPage : Page
    {
        public OutputPage()
        {
            this.InitializeComponent();
        }

        private async void OutputBtn_Click(object sender, RoutedEventArgs e)
        {
            // 获取选择的项
            string choice = (string)FormatComboBox.SelectedItem;

            if (choice == null)
            {
                //FormatTip.IsOpen = true;
                ContentDialog dialog = new ContentDialog
                {
                    Title = "导出错误",
                    Content = "请选择导出格式！",
                    CloseButtonText = "好的"
                };
                await dialog.ShowAsync();
            }
            else
            {
                var savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

                // 文件类型
                if (choice == "TIFF (*.tiff)")
                    savePicker.FileTypeChoices.Add("TIFF 文件", new List<string>() { ".tiff" });
                else if (choice == "PNG (*.png)")
                    savePicker.FileTypeChoices.Add("PNG 文件", new List<string>() { ".png" });

                // 默认文件名称
                if (App.mindCanvasFile.file == null)
                    savePicker.SuggestedFileName = "MindCanvas 思维导图";
                else
                    savePicker.SuggestedFileName = App.mindCanvasFile.file.DisplayName;

                StorageFile file = await savePicker.PickSaveFileAsync();

                if (file != null)
                {
                    var pixels = Snapshot.pixels;
                    var renderTargetBitmap = Snapshot.renderTargetBitmap;

                    // TIFF格式
                    if (choice == "TIFF (*.tiff)")
                    {
                        using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.TiffEncoderId, stream);
                            encoder.SetPixelData(
                                BitmapPixelFormat.Bgra8,
                                BitmapAlphaMode.Straight,
                                (uint)renderTargetBitmap.PixelWidth,
                                (uint)renderTargetBitmap.PixelHeight,
                                Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi,
                                Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi,
                                pixels.ToArray());
                            await encoder.FlushAsync();
                        }
                    }

                    // PNG格式
                    else if (choice == "PNG (*.png)")
                    {
                        using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                            encoder.SetPixelData(
                                BitmapPixelFormat.Bgra8,
                                BitmapAlphaMode.Straight,
                                (uint)renderTargetBitmap.PixelWidth,
                                (uint)renderTargetBitmap.PixelHeight,
                                Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi,
                                Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi,
                                pixels.ToArray());
                            await encoder.FlushAsync();
                        }
                    }
                }

            }
        }

        public async void MindMapBorder_Loaded(object sender, RoutedEventArgs e)
        {
            MindMapCanvas mindMapCanvas = new MindMapCanvas(showAnimation: false);
            MindMapBorder.Child = mindMapCanvas;
            mindMapCanvas.SetMindMap(App.mindMap);
            mindMapCanvas.DrawAll();

            await Snapshot.NewSnapshot(mindMapCanvas);
            PreviewImage.Source = Snapshot.renderTargetBitmap;

            // 防止PreviewImage自动缩放预览大小
            if (Snapshot.renderTargetBitmap.PixelWidth < PreviewImage.MaxWidth)
                PreviewImage.MaxWidth = Snapshot.renderTargetBitmap.PixelWidth;


            MindMapBorder.Visibility = Visibility.Collapsed;
        }
    }
}
