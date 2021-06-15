using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace MindCanvas
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private bool isLanguageComboBoxLoaded = false;// 判断LanguageComboBox是否加载好

        public SettingsPage()
        {
            this.InitializeComponent();

            // 主题
            string currentTheme = Settings.Theme;

            (ThemePanel.Children.Cast<RadioButton>().FirstOrDefault(c => c?.Tag?.ToString() == currentTheme)).IsChecked = true;

            // 语言
            string currentLanguage = Settings.Language;

            foreach (ComboBoxItem item in LanguageComboBox.Items)
                if (item.Tag.ToString() == currentLanguage)
                    LanguageComboBox.SelectedItem = item;

            isLanguageComboBoxLoaded = true;
        }

        // 设置主题
        private void OnThemeRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            var selectedTheme = ((RadioButton)sender)?.Tag?.ToString();

            Settings.Theme = selectedTheme;
        }

        // 设置语言
        private async void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isLanguageComboBoxLoaded)
                return;

            string selectedLanguage = ((ComboBoxItem)LanguageComboBox.SelectedItem).Tag.ToString();
            Settings.Language = selectedLanguage;

            await Dialog.Show.ChangeLanguageTip();
        }
    }
}
