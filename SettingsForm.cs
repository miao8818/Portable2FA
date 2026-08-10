using System;
using System.Drawing;
using System.Windows.Forms;

namespace Portable2FA
{
    internal sealed class SettingsForm : Form
    {
        private readonly CheckBox hotkeyToggle;
        private readonly CheckBox startupToggle;
        private readonly CheckBox syncToggle;
        private readonly HotkeyTextBox showHotkey;
        private readonly HotkeyTextBox captureHotkey;

        public AppSettings ResultSettings { get; private set; }

        public SettingsForm(AppSettings current)
        {
            Text = "设置";
            ClientSize = new Size(500, 438);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.White;
            Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;

            Controls.Add(NewLabel("设置", 24, 16, 220, 32, 15F,
                FontStyle.Bold, Palette.Ink));

            hotkeyToggle = NewToggle("启用全局快捷键", 24, 57);
            hotkeyToggle.Checked = current.EnableHotkey;
            Controls.Add(hotkeyToggle);

            Controls.Add(NewLabel("唤起窗口", 48, 98, 100, 28, 9F,
                FontStyle.Regular, Palette.Ink));
            showHotkey = NewHotkeyBox(280, 96);
            showHotkey.SetHotkey((Keys)current.ShowHotkeyModifiers,
                (Keys)current.ShowHotkeyKey);
            Controls.Add(showHotkey);

            Controls.Add(NewLabel("截图识别", 48, 138, 100, 28, 9F,
                FontStyle.Regular, Palette.Ink));
            captureHotkey = NewHotkeyBox(280, 136);
            captureHotkey.SetHotkey((Keys)current.CaptureHotkeyModifiers,
                (Keys)current.CaptureHotkeyKey);
            Controls.Add(captureHotkey);

            startupToggle = NewToggle("登录 Windows 后自动启动", 24, 181);
            startupToggle.Checked = current.StartWithWindows;
            Controls.Add(startupToggle);

            Panel divider = new Panel();
            divider.Location = new Point(24, 226);
            divider.Size = new Size(452, 1);
            divider.BackColor = Palette.Border;
            Controls.Add(divider);

            Controls.Add(NewLabel("跨设备同步", 24, 244, 220, 27, 11F,
                FontStyle.Bold, Palette.Ink));
            syncToggle = NewToggle("使用 Windows 凭据保管库同步", 24, 280);
            syncToggle.Checked = current.WindowsSync;
            syncToggle.Enabled = WindowsCredentialSync.IsAvailable();
            Controls.Add(syncToggle);

            Controls.Add(NewLabel("随 Microsoft 账户自动漫游，最多 20 个账户",
                48, 311, 410, 22, 8.5F, FontStyle.Regular, Palette.Muted));

            Button cancel = NewButton("取消", 316, 383, 76, false);
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);

            Button save = NewButton("保存", 400, 383, 76, true);
            save.Click += SaveClick;
            Controls.Add(save);

            AcceptButton = save;
            CancelButton = cancel;
        }

        private void SaveClick(object sender, EventArgs e)
        {
            if (showHotkey.HotkeyKey == captureHotkey.HotkeyKey &&
                showHotkey.HotkeyModifiers == captureHotkey.HotkeyModifiers)
            {
                MessageBox.Show(this, "两个功能不能使用相同的快捷键。", "快捷键",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ResultSettings = new AppSettings();
            ResultSettings.EnableHotkey = hotkeyToggle.Checked;
            ResultSettings.StartWithWindows = startupToggle.Checked;
            ResultSettings.WindowsSync = syncToggle.Checked;
            ResultSettings.ShowHotkeyKey = (int)showHotkey.HotkeyKey;
            ResultSettings.ShowHotkeyModifiers = (int)showHotkey.HotkeyModifiers;
            ResultSettings.CaptureHotkeyKey = (int)captureHotkey.HotkeyKey;
            ResultSettings.CaptureHotkeyModifiers = (int)captureHotkey.HotkeyModifiers;
            try
            {
                StartupManager.SetEnabled(ResultSettings.StartWithWindows);
                ResultSettings.Save();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "保存设置失败：" + ex.Message, "设置",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static HotkeyTextBox NewHotkeyBox(int x, int y)
        {
            HotkeyTextBox input = new HotkeyTextBox();
            input.Location = new Point(x, y);
            input.Size = new Size(196, 28);
            input.Font = new Font("Segoe UI", 10F);
            return input;
        }

        private static CheckBox NewToggle(string text, int x, int y)
        {
            CheckBox toggle = new CheckBox();
            toggle.Text = text;
            toggle.Location = new Point(x, y);
            toggle.Size = new Size(330, 30);
            toggle.Font = new Font("Microsoft YaHei UI", 9.5F);
            toggle.ForeColor = Palette.Ink;
            toggle.Cursor = Cursors.Hand;
            return toggle;
        }

        private static Button NewButton(string text, int x, int y, int width, bool primary)
        {
            Button button = new Button();
            button.Text = text;
            button.Location = new Point(x, y);
            button.Size = new Size(width, 34);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = Palette.Border;
            button.BackColor = primary ? Palette.Accent : Color.White;
            button.ForeColor = primary ? Color.White : Palette.Ink;
            button.Cursor = Cursors.Hand;
            return button;
        }

        private static Label NewLabel(string text, int x, int y, int width, int height,
            float size, FontStyle style, Color color)
        {
            Label label = new Label();
            label.Text = text;
            label.Location = new Point(x, y);
            label.Size = new Size(width, height);
            label.Font = new Font("Microsoft YaHei UI", size, style);
            label.ForeColor = color;
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }
    }
}
