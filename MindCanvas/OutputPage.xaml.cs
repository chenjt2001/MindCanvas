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

            PreviewImage.Source = Snapshot.renderTargetBitmap;
        }

        private async void OutputBtn_Click(object sender, RoutedEventArgs e)
        {
            // 获取选择的项
            string choice = (string)FormatComboBox.SelectedItem;

            if (choice == null)
            {
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
                if (choice == "JPEG (*.jpg)")
                    savePicker.FileTypeChoices.Add("JPEG 文件", new List<string>() { ".jpg" });
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
                    var pixels = Snapshot.canvasSnapshot;
                    var renderTargetBitmap = Snapshot.renderTargetBitmap;

                    // JPEG格式
                    if (choice == "JPEG (*.jpg)")
                    {
                        using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                            encoder.SetPixelData(
                                BitmapPixelFormat.Bgra8,
                                BitmapAlphaMode.Ignore,
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
    }
}
