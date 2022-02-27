using System;
using System.Threading.Tasks;

using Windows.UI.Xaml.Controls;

namespace MindCanvas.Dialog
{
    static class Show
    {
        private static ContentDialog dialog = null;

        /// <summary>显示版本新功能</summary>
        public static async Task ShowNewFunction()
        {
            if (dialog == null)
            {
                dialog = new ShowNewFunction();
                await dialog.ShowAsync();
                dialog = null;
            }
        }

        /// <summary>询问是否保存</summary>
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

        /// <summary>询问是否保存设置</summary>
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

        /// <summary>导出失败</summary>
        public async static Task ExportError()
        {
            if (dialog == null)
            {
                dialog = new ExportError();
                await dialog.ShowAsync();
                dialog = null;
            }
        }

        /// <summary>文件打开失败</summary>
        public async static Task OpenFileError()
        {
            if (dialog == null)
            {
                dialog = new OpenFileError();
                await dialog.ShowAsync();
                dialog = null;
            }
        }

        /// <summary>修改了语言设置</summary>
        public async static Task ChangeLanguageTip()
        {
            if (dialog == null)
            {
                dialog = new ChangeLanguageTip();
                await dialog.ShowAsync();
                dialog = null;
            }
        }

        /// <summary>输入密码</summary>
        public async static Task<string[]> EnterPassword(EnterPassword.Mode mode)
        {
            if (dialog == null)
            {
                string[] result;
                dialog = new ContentDialog();// 占用住dialog
                EnterPassword trueDialog = new EnterPassword(mode);
                result = await trueDialog.GetResult();
                dialog = null;
                return result;
            }
            return null;
        }
    }
}
