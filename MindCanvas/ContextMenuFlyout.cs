using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.System;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Input;

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
                fontIcon.FontFamily = new FontFamily("Segoe MDL2 Assets");
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
