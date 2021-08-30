using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace MindCanvas
{
    class ContextMenuFlyout : MenuFlyout
    {
        public MenuFlyoutItem AddItem(string text, VirtualKey? key, VirtualKeyModifiers? modifiers, string iconGlyph = null)
        {
            MenuFlyoutItem item = new MenuFlyoutItem();
            item.Text = text;

            if (iconGlyph != null)
            {
                FontIcon fontIcon = new FontIcon();
                fontIcon.FontFamily = Application.Current.Resources["SymbolThemeFontFamily"] as FontFamily;
                fontIcon.Glyph = iconGlyph;
                item.Icon = fontIcon;
            }

            KeyboardAccelerator keyboardAccelerator = new KeyboardAccelerator();
            keyboardAccelerator.Key = key ?? VirtualKey.None;
            keyboardAccelerator.Modifiers = modifiers ?? VirtualKeyModifiers.None;
            item.KeyboardAccelerators.Add(keyboardAccelerator);

            Items.Add(item);

            return item;
        }


    }
}
