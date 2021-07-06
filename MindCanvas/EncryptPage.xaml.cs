using Microsoft.UI.Xaml.Controls;
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
        public EncryptPage()
        {
            this.InitializeComponent();

            ToggleEnableEncryptionToggleSwitch(App.mindCanvasFile.IsEncrypted);
        }

        // 修改密码
        private async void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string[] result = await Dialog.Show.EnterPassword(Dialog.EnterPassword.Mode.ChangePassword);

            if (result == null)// 取消
                return;

            if (result[1] != result[2])
                InfoHelper.ShowInfoBar("修改密码失败，两次密码不一致！", InfoBarSeverity.Error);
            else
            {
                if (App.mindCanvasFile.SetPassword(result[0], result[1]))
                    InfoHelper.ShowInfoBar("密码已修改", InfoBarSeverity.Success);
                else
                    InfoHelper.ShowInfoBar("修改密码失败，旧密码错误！", InfoBarSeverity.Error);
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
                    InfoHelper.ShowInfoBar("设置密码失败，两次密码不一致！", InfoBarSeverity.Error);
                    ToggleEnableEncryptionToggleSwitch(false);
                }
                else
                {
                    if (App.mindCanvasFile.SetPassword(null, result[0]))
                        InfoHelper.ShowInfoBar("密码已设置", InfoBarSeverity.Success);
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

                if (App.mindCanvasFile.SetPassword(result[0], null))
                    InfoHelper.ShowInfoBar("密码已关闭", InfoBarSeverity.Success);
                else
                {
                    InfoHelper.ShowInfoBar("关闭密码失败，密码错误！", InfoBarSeverity.Error);
                    ToggleEnableEncryptionToggleSwitch(true);
                }
            }
        }

        // 用代码切换EnableEncryptionToggleSwitch时调用，防止执行事件函数
        private void ToggleEnableEncryptionToggleSwitch(bool value)
        {
            EnableEncryptionToggleSwitch.Toggled -= EnableEncryptionToggleSwitch_Toggled;
            EnableEncryptionToggleSwitch.IsOn = value;
            EnableEncryptionToggleSwitch.Toggled += EnableEncryptionToggleSwitch_Toggled;
        }
    }
}
