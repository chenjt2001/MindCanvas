using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Xaml.Controls;

namespace MindCanvas
{
    static class Dialog
    {
        public static async Task ShowNewFunction()
        {
            // To get if the app is being used for the first time since it was installed.
            //bool isFirstUse = SystemInformation.IsFirstRun;
            // To get if the app is being used for the first time since being upgraded from an older version.
            //bool IsAppUpdated = SystemInformation.IsAppUpdated;

            //if (isFirstUse || IsAppUpdated)
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = "新功能",
                    Content = @"1、墨迹书写 - 启用“墨迹书写”模式即可书写。如果您未使用Pen，请保持“触控书写”功能开启。
2、修改线的描述 - 单击两点之间的线即可修改线的描述。
3、更丰富的导出格式 - 支持导出为JPEG、PNG和HEIC格式。
4、修改点默认的边框颜色和文字大小 - 依次单击“…”“设置默认值”进入“默认值设置”界面。",
                    CloseButtonText = "好的"
                };

                await dialog.ShowAsync();
            }
        }

        // 询问是否保存
        public async static Task<ContentDialogResult> AskForSave()
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = "是否希望保存您的思维导图？",
                PrimaryButtonText = "保存",
                SecondaryButtonText = "不保存",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,

            };
            ContentDialogResult result = await dialog.ShowAsync();
            return result;
        }

        // 导出失败
        public async static Task<ContentDialogResult> AskForSaveSettings()
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = "是否希望保存您的设置？",
                PrimaryButtonText = "保存",
                SecondaryButtonText = "不保存",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,

            };
            ContentDialogResult result = await dialog.ShowAsync();
            return result;
        }

        // 询问是否保存设置
        public async static Task OutputError()
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "导出错误",
                Content = "请确认已选择导出格式且已安装此格式对应的的编解码器！",
                CloseButtonText = "好的"
            };
            await dialog.ShowAsync();
        }
    }
}
