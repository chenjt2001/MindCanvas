using System;
using System.Threading.Tasks;

using Windows.UI.Xaml.Controls;

namespace MindCanvas.Dialog
{
    static class Show
    {
        public static async Task ShowNewFunction()
        {
            // To get if the app is being used for the first time since it was installed.
            //bool isFirstUse = SystemInformation.IsFirstRun;
            // To get if the app is being used for the first time since being upgraded from an older version.
            //bool IsAppUpdated = SystemInformation.IsAppUpdated;

            //if (isFirstUse || IsAppUpdated)
            {
                ShowNewFunction dialog = new ShowNewFunction();
                await dialog.ShowAsync();
            }
        }

        // 询问是否保存
        public async static Task<ContentDialogResult> AskForSave()
        {
            AskForSave dialog = new AskForSave();
            ContentDialogResult result = await dialog.ShowAsync();
            return result;
        }

        // 询问是否保存设置
        public async static Task<ContentDialogResult> AskForSaveSettings()
        {
            AskForSaveSettings dialog = new AskForSaveSettings();
            ContentDialogResult result = await dialog.ShowAsync();
            return result;
        }

        // 导出失败
        public async static Task ExportError()
        {
            ExportError dialog = new ExportError();
            await dialog.ShowAsync();
        }

        // 文件打开失败
        public async static Task OpenFileError()
        {
            OpenFileError dialog = new OpenFileError();
            await dialog.ShowAsync();
        }

        // 修改了语言设置
        public async static Task ChangeLanguageTip()
        {
            ChangeLanguageTip dialog = new ChangeLanguageTip();
            await dialog.ShowAsync();
        }
    }
}
