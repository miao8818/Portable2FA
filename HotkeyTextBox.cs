using System;
using System.Windows.Forms;

namespace Portable2FA
{
    internal sealed class HotkeyTextBox : TextBox
    {
        public Keys HotkeyKey { get; private set; }
        public Keys HotkeyModifiers { get; private set; }

        public HotkeyTextBox()
        {
            ReadOnly = true;
            ShortcutsEnabled = false;
            Cursor = Cursors.Hand;
            TextAlign = HorizontalAlignment.Center;
            KeyDown += CaptureKeyDown;
        }

        public void SetHotkey(Keys modifiers, Keys key)
        {
            HotkeyModifiers = modifiers & (Keys.Control | Keys.Alt | Keys.Shift);
            HotkeyKey = key & Keys.KeyCode;
            Text = FormatHotkey(HotkeyModifiers, HotkeyKey);
        }

        private void CaptureKeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
            e.Handled = true;
            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.Menu ||
                e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.LWin ||
                e.KeyCode == Keys.RWin)
                return;
            Keys modifiers = e.Modifiers & (Keys.Control | Keys.Alt | Keys.Shift);
            if (modifiers == Keys.None)
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }
            SetHotkey(modifiers, e.KeyCode);
        }

        public static string FormatHotkey(Keys modifiers, Keys key)
        {
            string value = string.Empty;
            if ((modifiers & Keys.Control) == Keys.Control) value += "Ctrl + ";
            if ((modifiers & Keys.Alt) == Keys.Alt) value += "Alt + ";
            if ((modifiers & Keys.Shift) == Keys.Shift) value += "Shift + ";
            return value + key.ToString();
        }
    }
}
