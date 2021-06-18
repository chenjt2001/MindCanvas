using System.Linq;
using Windows.Globalization;
using Windows.Storage;
using Windows.System.UserProfile;
using Windows.UI.Xaml;

namespace MindCanvas
{
    public static class Settings
    {
        private static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        // 应用设置
        public static void Apply()
        {
            Theme = Theme;
            Language = Language;
        }

        // 清除设置
        public static void Clear()
        {
            ApplicationData.Current.LocalSettings.Values.Clear();
            ApplicationLanguages.PrimaryLanguageOverride = GlobalizationPreferences.Languages.First();
        }

        // 主题
        public static string Theme
        {
            get
            {
                return localSettings.Values["Theme"] == null ? "Default" : localSettings.Values["Theme"].ToString();
                // return ThemeHelper.RootTheme.ToString();
            }

            set
            {
                ThemeHelper.RootTheme = App.GetEnum<ElementTheme>(value);
                MainPage.mainPage.RefreshTheme();
                localSettings.Values["Theme"] = value;
            }

        }

        // 语言
        public static string Language
        {
            get
            {
                return localSettings.Values["Language"] == null ? "Default" : localSettings.Values["Language"].ToString();
            }

            set
            {
                string languageTag;
                languageTag = value == "Default" ? GlobalizationPreferences.Languages.First() : value;

                ApplicationLanguages.PrimaryLanguageOverride = languageTag;
                localSettings.Values["Language"] = value;
            }
        }

        // 总启动次数
        public static long TotalLaunchCount
        {
            get
            {
                return localSettings.Values["TotalLaunchCount"] == null ? 0 : (long)localSettings.Values["TotalLaunchCount"];
            }
            set
            {
                localSettings.Values["TotalLaunchCount"] = value;
            }
        }

        // 上一错误
        public static string LastError
        {
            get
            {
                return localSettings.Values["LastError"]?.ToString();
            }
            set
            {
                localSettings.Values["LastError"] = value;
            }
        }
    }
}
