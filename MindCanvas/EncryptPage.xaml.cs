using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace MindCanvas
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class EncryptPage : Page
    {
        // 资源加载器，用于翻译
        private static readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

        public EncryptPage()
        {
            this.InitializeComponent();

            ToggleEnableEncryptionToggleSwitch(App.mindCanvasFile.IsEncrypted);
        }

        /// <summary>修改密码</summary>
        private async void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string[] result = await Dialog.Show.EnterPassword(Dialog.EnterPassword.Mode.ChangePassword);

            if (result == null)// 取消
                return;

            if (result[1] != result[2])
                InfoHelper.ShowInfoBar(resourceLoader.GetString("Code_EncryptPage1"), InfoBarSeverity.Error);// 修改密码失败，两次密码不一致！
            else
            {
                if (await App.mindCanvasFile.SetPassword(result[0], result[1]))
                    InfoHelper.ShowInfoBar(resourceLoader.GetString("Code_EncryptPage2"), InfoBarSeverity.Success);// 密码已修改
                else
                    InfoHelper.ShowInfoBar(resourceLoader.GetString("Code_EncryptPage3"), InfoBarSeverity.Error);// 修改密码失败，旧密码错误！
            }
        }

        private async void EnableEncryptionToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (EnableEncryptionToggleSwitch.IsOn)// 打开密码
            {
                string[] result = await Dialog.Show.EnterPassword(Dialog.EnterPassword.Mode.SetPassword);

                if (result == null)// 取消
                {
                    ToggleEnableEncryptionToggleSwitch(false);
                    return;
                }

                if (result[0] != result[1])
                {
                    InfoHelper.ShowInfoBar(resourceLoader.GetString("Code_EncryptPage4"), InfoBarSeverity.Error);// 设置密码失败，两次密码不一致！
                    ToggleEnableEncryptionToggleSwitch(false);
                }
                else
                {
                    if (await App.mindCanvasFile.SetPassword(null, result[0]))
                        InfoHelper.ShowInfoBar(resourceLoader.GetString("Code_EncryptPage5"), InfoBarSeverity.Success);// 密码已设置
                }
            }

            else// 关闭密码
            {
                string[] result = await Dialog.Show.EnterPassword(Dialog.EnterPassword.Mode.RequirePassword);

                if (result == null)// 取消
                {
                    ToggleEnableEncryptionToggleSwitch(true);
                    return;
                }

                if (await App.mindCanvasFile.SetPassword(result[0], null))
                    InfoHelper.ShowInfoBar(resourceLoader.GetString("Code_EncryptPage6"), InfoBarSeverity.Success);// 密码已关闭
                else
                {
                    InfoHelper.ShowInfoBar(resourceLoader.GetString("Code_EncryptPage7"), InfoBarSeverity.Error);// 关闭密码失败，密码错误！
                    ToggleEnableEncryptionToggleSwitch(true);
                }
            }
        }

        /// <summary>用代码切换EnableEncryptionToggleSwitch时调用，防止执行事件函数</summary>
        private void ToggleEnableEncryptionToggleSwitch(bool value)
        {
            EnableEncryptionToggleSwitch.Toggled -= EnableEncryptionToggleSwitch_Toggled;
            EnableEncryptionToggleSwitch.IsOn = value;
            EnableEncryptionToggleSwitch.Toggled += EnableEncryptionToggleSwitch_Toggled;
        }
    }
}
