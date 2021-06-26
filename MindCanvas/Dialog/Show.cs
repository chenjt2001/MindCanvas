using System;
using System.Threading.Tasks;

using Windows.UI.Xaml.Controls;

namespace MindCanvas.Dialog
{
    static class Show
    {
        private static ContentDialog dialog = null;

        // 显示版本新功能
        public static async Task ShowNewFunction()
        {
            if (dialog == null)
            {
                dialog = new ShowNewFunction();
                await dialog.ShowAsync();
                dialog = null;
            }
        }

        // 询问是否保存
        public async static Task<ContentDialogResult> AskForSave()
        {
            if (dialog == null)
            {
                dialog = new AskForSave();
                ContentDialogResult result = await dialog.ShowAsync();
                dialog = null;
                return result;
            }
            return ContentDialogResult.None;
        }

        // 询问是否保存设置
        public async static Task<ContentDialogResult> AskForSaveSettings()
        {
            if (dialog == null)
            {
                dialog = new AskForSaveSettings();
                ContentDialogResult result = await dialog.ShowAsync();
                dialog = null;
                return result;
            }
            return ContentDialogResult.None;
        }

        // 导出失败
        public async static Task ExportError()
        {
            if (dialog == null)
            {
                dialog = new ExportError();
                await dialog.ShowAsync();
                dialog = null;
            }
        }

        // 文件打开失败
        public async static Task OpenFileError()
        {
            if (dialog == null)
            {
                dialog = new OpenFileError();
                await dialog.ShowAsync();
                dialog = null;
            }
        }

        // 修改了语言设置
        public async static Task ChangeLanguageTip()
        {
            if (dialog == null)
            {
                dialog = new ChangeLanguageTip();
                await dialog.ShowAsync();
                dialog = null;
            }
        }
    }
}
