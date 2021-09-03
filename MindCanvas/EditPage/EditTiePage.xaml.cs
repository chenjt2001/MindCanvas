using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace MindCanvas.EditPage
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class EditTiePage : Page
    {
        private Tie tie;
        private Windows.UI.Xaml.Shapes.Path path;
        private MainPage mainPage;

        public EditTiePage()
        {
            this.InitializeComponent();
            Loaded += EditTiePage_Loaded;
        }

        private void EditTiePage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            tie = e.Parameter as Tie;
        }

        private void LoadData()
        {
            path = MainPage.mindMapCanvas.ConvertTieToPath(tie);
            mainPage = MainPage.mainPage;

            // 描述
            DescriptionTextBox.Text = tie.Description;

            // 边框颜色
            UseDefaultStrokeToggleSwitch.IsOn = tie.StrokeArgb == null;
            UseDefaultStrokeToggleSwitch_Toggled(UseDefaultStrokeToggleSwitch, null);
        }

        /// <summary>描述文本框失去焦点</summary>
        private void DescriptionTextBox_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            // 如果已修改，则修改点描述
            if (DescriptionTextBox.Text != tie.Description)
            {
                EventsManager.ModifyTie(tie, DescriptionTextBox.Text);
                mainPage.RefreshUnRedoBtn();
            }
        }

        private void RemoveTieBtn_Click(object sender, RoutedEventArgs e)
        {
            EventsManager.RemoveTie(tie);
            mainPage.RefreshUnRedoBtn();
            mainPage.ShowFrame(typeof(EditPage.InfoPage));
        }

        private void UseDefaultStrokeToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ColorPickerViewbox.Visibility = UseDefaultStrokeToggleSwitch.IsOn ? Visibility.Collapsed : Visibility.Visible;

            // 默认边框颜色
            if (UseDefaultStrokeToggleSwitch.IsOn)
            {
                if (tie.StrokeArgb != null)
                {
                    EventsManager.ModifyTieStrokeColor(tie, null);
                    mainPage.RefreshUnRedoBtn();
                }
            }

            // 自定义边框颜色
            else
            {
                StrokeColorPicker.Color = (path.Stroke as SolidColorBrush).Color;

                if (tie.StrokeArgb == null)
                {
                    EventsManager.ModifyTieStrokeColor(tie, StrokeColorPicker.Color);
                    mainPage.RefreshUnRedoBtn();
                }
            }
        }

        /// <summary>使颜色跟随当前值改变</summary>
        private void StrokeColorPicker_ColorChanged(Microsoft.UI.Xaml.Controls.ColorPicker sender, Microsoft.UI.Xaml.Controls.ColorChangedEventArgs args)
        {
            path.Stroke = new SolidColorBrush(StrokeColorPicker.Color);
        }

        /// <summary>鼠标释放，完成颜色修改</summary>
        private void StrokeColorPicker_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            if (tie.StrokeArgb != null)
            {
                if (StrokeColorPicker.Color.A != tie.StrokeArgb[0] ||
                    StrokeColorPicker.Color.R != tie.StrokeArgb[1] ||
                    StrokeColorPicker.Color.G != tie.StrokeArgb[2] ||
                    StrokeColorPicker.Color.B != tie.StrokeArgb[3])
                {
                    EventsManager.ModifyTieStrokeColor(tie, StrokeColorPicker.Color);
                    mainPage.RefreshUnRedoBtn();
                }
            }
            else
            {
                EventsManager.ModifyTieStrokeColor(tie, StrokeColorPicker.Color);
                mainPage.RefreshUnRedoBtn();
            }
        }
    }
}
