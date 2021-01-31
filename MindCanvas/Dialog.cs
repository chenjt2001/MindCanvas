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
                    Title = "新功能 - 无限画布",
                    Content = "使用 鼠标 或 手指 在画布上拖动，可使用画布的其他区域。双指捏合 或 按住Ctrl键并转动鼠标滚轮 可缩放画布。",
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
    }
}
