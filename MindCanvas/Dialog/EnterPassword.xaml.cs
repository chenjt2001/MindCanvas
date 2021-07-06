using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace MindCanvas.Dialog
{
    public sealed partial class EnterPassword : ContentDialog
    {
        // 资源加载器，用于翻译
        private readonly Mode mode;
        private static readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

        public enum Mode { RequirePassword, SetPassword, ChangePassword };

        public EnterPassword(Mode mode)
        {
            this.InitializeComponent();

            this.mode = mode;

            CheckPasswordBox();

            switch (mode)
            {
                case Mode.ChangePassword:// 修改密码
                    PasswordBox0.Header = resourceLoader.GetString("Code_PleaseEnterTheOldPassword"); // 请输入旧密码：
                    PasswordBox0.Visibility = Visibility.Visible;
                    PasswordBox1.Header = resourceLoader.GetString("Code_PleaseEnterTheNewPassword"); // 请输入新密码：
                    PasswordBox1.Visibility = Visibility.Visible;
                    PasswordBox2.Header = resourceLoader.GetString("Code_PleaseEnterTheNewPasswordAgain"); // 请再次输入新密码：
                    PasswordBox2.Visibility = Visibility.Visible;
                    break;

                case Mode.RequirePassword:// 需要密码
                    PasswordBox0.Header = resourceLoader.GetString("Code_PleaseEnterThePassword"); // 请输入密码：
                    PasswordBox0.Visibility = Visibility.Visible;
                    break;

                case Mode.SetPassword:// 设置密码
                    PasswordBox0.Header = resourceLoader.GetString("Code_PleaseEnterThePassword"); // 请输入密码：
                    PasswordBox0.Visibility = Visibility.Visible;
                    PasswordBox1.Header = resourceLoader.GetString("Code_PleaseEnterThePasswordAgain"); // 请再次输入密码：
                    PasswordBox1.Visibility = Visibility.Visible;
                    break;
            }
        }

        public async Task<string[]> GetResult()
        {
            ContentDialogResult choice = await ShowAsync();

            if (choice == ContentDialogResult.Secondary || choice == ContentDialogResult.None)
                return null;

            string[] result = { PasswordBox0.Password, PasswordBox1.Password, PasswordBox2.Password };
            return result;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            CheckPasswordBox();
        }

        private void CheckPasswordBox()
        {
            switch (mode)
            {
                case Mode.ChangePassword:// 修改密码
                    IsPrimaryButtonEnabled = PasswordBox0.Password != string.Empty && PasswordBox1.Password != string.Empty && PasswordBox2.Password != string.Empty;
                    break;

                case Mode.RequirePassword:// 需要密码
                    IsPrimaryButtonEnabled = PasswordBox0.Password != string.Empty;
                    break;

                case Mode.SetPassword:// 设置密码
                    IsPrimaryButtonEnabled = PasswordBox0.Password != string.Empty && PasswordBox1.Password != string.Empty;
                    break;
            }
        }
    }
}
