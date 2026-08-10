using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Portable2FA
{
    internal sealed class MainForm : Form
    {
        private readonly TextBox secretInput;
        private readonly Label secretPlaceholder;
        private readonly Label codeLabel;
        private readonly Label accountLabel;
        private readonly Label detailLabel;
        private readonly Label statusLabel;
        private readonly CountdownRing countdownRing;
        private readonly ActionButton copyButton;
        private readonly IconButton visibilityButton;
        private readonly Timer refreshTimer;
        private readonly NotifyIcon trayIcon;
        private readonly ToolStripMenuItem trayCopyItem;
        private readonly ToolTip toolTip;

        private TotpProfile currentProfile;
        private string currentCode;
        private bool exiting;
        private bool trayHintShown;
        private DateTime copyFeedbackUntil;

        public MainForm()
        {
            Text = "Portable 2FA";
            ClientSize = new Size(520, 526);
            MinimumSize = MaximumSize = new Size(536, 565);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Palette.Canvas;
            Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi;
            KeyPreview = true;

            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { }

            Panel header = BuildHeader();
            Controls.Add(header);

            Label inputTitle = NewLabel("身份验证密钥", 24, 108, 180, 24, 10F, FontStyle.Bold, Palette.Ink);
            Controls.Add(inputTitle);

            RoundedPanel inputSurface = new RoundedPanel();
            inputSurface.Location = new Point(24, 137);
            inputSurface.Size = new Size(472, 48);
            inputSurface.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(inputSurface);

            secretInput = new TextBox();
            secretInput.BorderStyle = BorderStyle.None;
            secretInput.Font = new Font("Segoe UI", 11F);
            secretInput.Location = new Point(14, 13);
            secretInput.Size = new Size(370, 25);
            secretInput.UseSystemPasswordChar = true;
            secretInput.BackColor = Color.White;
            secretInput.TextChanged += delegate
            {
                UpdateSecretPlaceholder();
                ParseInput();
            };
            secretInput.KeyDown += SecretInputKeyDown;
            secretInput.Enter += delegate { UpdateSecretPlaceholder(); };
            secretInput.Leave += delegate { UpdateSecretPlaceholder(); };
            inputSurface.Controls.Add(secretInput);

            secretPlaceholder = NewLabel("粘贴 Base32 密钥或 otpauth:// 链接",
                14, 11, 366, 26, 9.5F, FontStyle.Regular, Color.FromArgb(137, 147, 158));
            secretPlaceholder.Cursor = Cursors.IBeam;
            secretPlaceholder.Click += delegate { secretInput.Focus(); };
            inputSurface.Controls.Add(secretPlaceholder);
            secretPlaceholder.BringToFront();

            visibilityButton = new IconButton();
            visibilityButton.Kind = IconButton.IconKind.Eye;
            visibilityButton.Location = new Point(390, 5);
            visibilityButton.Size = new Size(36, 38);
            visibilityButton.Click += ToggleSecretVisibility;
            inputSurface.Controls.Add(visibilityButton);

            IconButton pasteButton = new IconButton();
            pasteButton.Kind = IconButton.IconKind.Paste;
            pasteButton.Location = new Point(428, 5);
            pasteButton.Size = new Size(36, 38);
            pasteButton.Click += PasteSecret;
            inputSurface.Controls.Add(pasteButton);

            toolTip = new ToolTip();
            toolTip.SetToolTip(visibilityButton, "显示密钥");
            toolTip.SetToolTip(pasteButton, "从剪贴板粘贴");

            RoundedPanel codeSurface = new RoundedPanel();
            codeSurface.Location = new Point(24, 201);
            codeSurface.Size = new Size(472, 244);
            codeSurface.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(codeSurface);

            Label codeTitle = NewLabel("动态验证码", 20, 18, 150, 24, 10F, FontStyle.Bold, Palette.Ink);
            codeSurface.Controls.Add(codeTitle);

            accountLabel = NewLabel("", 20, 43, 322, 20, 8.5F, FontStyle.Regular, Palette.Muted);
            accountLabel.AutoEllipsis = true;
            codeSurface.Controls.Add(accountLabel);

            codeLabel = NewLabel("--- ---", 20, 70, 334, 66, 40F, FontStyle.Bold, Palette.Ink);
            codeLabel.Font = new Font("Consolas", 40F, FontStyle.Bold);
            codeLabel.Cursor = Cursors.Hand;
            codeLabel.TextAlign = ContentAlignment.MiddleLeft;
            codeLabel.Click += delegate { CopyCurrentCode(); };
            codeSurface.Controls.Add(codeLabel);
            toolTip.SetToolTip(codeLabel, "点击复制验证码");

            countdownRing = new CountdownRing();
            countdownRing.Location = new Point(365, 52);
            countdownRing.Size = new Size(88, 88);
            codeSurface.Controls.Add(countdownRing);

            detailLabel = NewLabel("SHA1  ·  6 位  ·  30 秒", 20, 139, 250, 22, 8.5F, FontStyle.Regular, Palette.Muted);
            codeSurface.Controls.Add(detailLabel);

            copyButton = new ActionButton();
            copyButton.Text = "复制验证码";
            copyButton.Location = new Point(20, 179);
            copyButton.Size = new Size(190, 46);
            copyButton.Enabled = false;
            copyButton.Click += delegate { CopyCurrentCode(); };
            codeSurface.Controls.Add(copyButton);

            Label shortcut = NewLabel("也可直接点击验证码", 226, 190, 210, 24, 8.5F, FontStyle.Regular, Palette.Muted);
            shortcut.TextAlign = ContentAlignment.MiddleRight;
            codeSurface.Controls.Add(shortcut);

            statusLabel = NewLabel("等待输入密钥", 24, 463, 472, 25, 9F, FontStyle.Regular, Palette.Muted);
            Controls.Add(statusLabel);

            Label privacyLabel = NewLabel("密钥仅在内存中使用", 24, 493, 472, 18, 8F, FontStyle.Regular, Color.FromArgb(136, 146, 158));
            privacyLabel.TextAlign = ContentAlignment.MiddleRight;
            Controls.Add(privacyLabel);

            ContextMenuStrip trayMenu = new ContextMenuStrip();
            trayMenu.Font = new Font("Microsoft YaHei UI", 9F);
            ToolStripMenuItem showItem = new ToolStripMenuItem("显示窗口");
            showItem.Font = new Font(trayMenu.Font, FontStyle.Bold);
            showItem.Click += delegate { RestoreFromTray(); };
            trayCopyItem = new ToolStripMenuItem("复制当前验证码");
            trayCopyItem.Enabled = false;
            trayCopyItem.Click += delegate { CopyCurrentCode(); };
            ToolStripMenuItem exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += ExitApplication;
            trayMenu.Items.Add(showItem);
            trayMenu.Items.Add(trayCopyItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(exitItem);

            trayIcon = new NotifyIcon();
            trayIcon.Text = "Portable 2FA";
            trayIcon.Icon = LoadTrayIcon();
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += delegate { RestoreFromTray(); };

            refreshTimer = new Timer();
            refreshTimer.Interval = 125;
            refreshTimer.Tick += RefreshTick;
            refreshTimer.Start();

            FormClosing += FormClosingToTray;
            Resize += MinimizeToTray;
            Shown += delegate
            {
                UpdateSecretPlaceholder();
            };
        }

        public void RestoreFromTray()
        {
            if (IsDisposed)
                return;
            Show();
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            Activate();
            BringToFront();
            secretInput.Focus();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.C) && !secretInput.Focused && currentProfile != null)
            {
                CopyCurrentCode();
                return true;
            }
            if (keyData == Keys.Escape)
            {
                HideToTray();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (refreshTimer != null) refreshTimer.Dispose();
                if (trayIcon != null) trayIcon.Dispose();
                if (toolTip != null) toolTip.Dispose();
            }
            base.Dispose(disposing);
        }

        private Panel BuildHeader()
        {
            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 88;
            header.BackColor = Palette.Navy;

            PictureBox mark = new PictureBox();
            mark.Location = new Point(24, 21);
            mark.Size = new Size(46, 46);
            mark.SizeMode = PictureBoxSizeMode.Zoom;
            try { mark.Image = Icon.ToBitmap(); } catch { }
            header.Controls.Add(mark);

            Label title = NewLabel("Portable 2FA", 84, 19, 300, 32, 18F, FontStyle.Bold, Color.White);
            header.Controls.Add(title);
            Label subtitle = NewLabel("快速、安全的动态验证码", 85, 51, 300, 22, 9F, FontStyle.Regular,
                Color.FromArgb(181, 196, 211));
            header.Controls.Add(subtitle);

            Label version = NewLabel("v" + BuildInfo.Version, 406, 25, 88, 20,
                8.5F, FontStyle.Bold, Color.FromArgb(221, 230, 238));
            version.TextAlign = ContentAlignment.MiddleRight;
            header.Controls.Add(version);

            string updatedAt = BuildInfo.UpdatedAt;
            if (updatedAt.Length >= 16)
                updatedAt = updatedAt.Substring(0, 16).Replace('T', ' ');
            Label updated = NewLabel("更新 " + updatedAt, 354, 48, 140, 19,
                7.5F, FontStyle.Regular, Color.FromArgb(152, 171, 189));
            updated.TextAlign = ContentAlignment.MiddleRight;
            header.Controls.Add(updated);
            return header;
        }

        private void ParseInput()
        {
            currentProfile = null;
            currentCode = null;

            if (string.IsNullOrWhiteSpace(secretInput.Text))
            {
                SetEmptyState("等待输入密钥", false);
                return;
            }

            try
            {
                currentProfile = Totp.ParseProfile(secretInput.Text);
                string account = currentProfile.AccountLabel;
                accountLabel.Text = string.IsNullOrEmpty(account) ? "标准 TOTP" : account;
                detailLabel.Text = string.Format("{0}  ·  {1} 位  ·  {2} 秒",
                    currentProfile.Algorithm, currentProfile.Digits, currentProfile.Period);
                statusLabel.Text = "验证码已就绪";
                statusLabel.ForeColor = Palette.Accent;
                copyButton.Enabled = true;
                trayCopyItem.Enabled = true;
                UpdateCode();
            }
            catch (FormatException ex)
            {
                SetEmptyState(ex.Message, true);
            }
        }

        private void UpdateSecretPlaceholder()
        {
            if (secretPlaceholder == null)
                return;
            secretPlaceholder.Visible = secretInput.TextLength == 0;
        }

        private void SetEmptyState(string message, bool error)
        {
            codeLabel.Text = "--- ---";
            accountLabel.Text = string.Empty;
            detailLabel.Text = "SHA1  ·  6 位  ·  30 秒";
            countdownRing.SetValue(0, 0);
            statusLabel.Text = message;
            statusLabel.ForeColor = error ? Palette.Error : Palette.Muted;
            copyButton.Enabled = false;
            trayCopyItem.Enabled = false;
        }

        private void RefreshTick(object sender, EventArgs e)
        {
            if (currentProfile != null)
                UpdateCode();

            if (copyFeedbackUntil != DateTime.MinValue && DateTime.UtcNow >= copyFeedbackUntil)
            {
                copyFeedbackUntil = DateTime.MinValue;
                copyButton.Text = "复制验证码";
                copyButton.Invalidate();
            }
        }

        private void UpdateCode()
        {
            long now = Totp.UnixTimeSeconds();
            string next = Totp.Generate(currentProfile, now);
            if (!string.Equals(currentCode, next, StringComparison.Ordinal))
            {
                currentCode = next;
                int split = next.Length / 2;
                codeLabel.Text = next.Substring(0, split) + " " + next.Substring(split);
                trayIcon.Text = "Portable 2FA  ·  " + codeLabel.Text;
            }

            double elapsed = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            double intoPeriod = elapsed % currentProfile.Period;
            double remaining = currentProfile.Period - intoPeriod;
            int seconds = Math.Max(1, (int)Math.Ceiling(remaining));
            countdownRing.SetValue(seconds, remaining / currentProfile.Period);
        }

        private void CopyCurrentCode()
        {
            if (string.IsNullOrEmpty(currentCode))
                return;
            try
            {
                Clipboard.SetText(currentCode);
                copyButton.Text = "已复制";
                copyButton.Invalidate();
                statusLabel.Text = "验证码已复制到剪贴板";
                statusLabel.ForeColor = Palette.Accent;
                copyFeedbackUntil = DateTime.UtcNow.AddSeconds(1.5);
            }
            catch (ExternalException)
            {
                statusLabel.Text = "剪贴板正被其他程序占用，请再试一次";
                statusLabel.ForeColor = Palette.Error;
            }
        }

        private void PasteSecret(object sender, EventArgs e)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    secretInput.Text = Clipboard.GetText().Trim();
                    secretInput.SelectionStart = secretInput.TextLength;
                    secretInput.Focus();
                }
            }
            catch (ExternalException)
            {
                statusLabel.Text = "剪贴板正被其他程序占用，请再试一次";
                statusLabel.ForeColor = Palette.Error;
            }
        }

        private void SecretInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                secretInput.SelectAll();
                e.SuppressKeyPress = true;
            }
        }

        private void ToggleSecretVisibility(object sender, EventArgs e)
        {
            int selection = secretInput.SelectionStart;
            secretInput.UseSystemPasswordChar = !secretInput.UseSystemPasswordChar;
            visibilityButton.Kind = secretInput.UseSystemPasswordChar
                ? IconButton.IconKind.Eye
                : IconButton.IconKind.EyeOff;
            toolTip.SetToolTip(visibilityButton,
                secretInput.UseSystemPasswordChar ? "显示密钥" : "隐藏密钥");
            visibilityButton.Invalidate();
            secretInput.SelectionStart = selection;
            secretInput.Focus();
        }

        private void FormClosingToTray(object sender, FormClosingEventArgs e)
        {
            if (exiting)
                return;
            e.Cancel = true;
            HideToTray();
        }

        private void MinimizeToTray(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
                HideToTray();
        }

        private void HideToTray()
        {
            Hide();
            ShowInTaskbar = false;
            if (!trayHintShown)
            {
                trayHintShown = true;
                trayIcon.BalloonTipTitle = "Portable 2FA 仍在运行";
                trayIcon.BalloonTipText = "双击托盘图标可恢复窗口";
                trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                trayIcon.ShowBalloonTip(2200);
            }
        }

        private void ExitApplication(object sender, EventArgs e)
        {
            exiting = true;
            trayIcon.Visible = false;
            Close();
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
            label.BackColor = Color.Transparent;
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private static Icon LoadTrayIcon()
        {
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("Portable2FA.TrayIcon"))
                {
                    if (stream != null)
                    {
                        using (Icon source = new Icon(stream))
                            return (Icon)source.Clone();
                    }
                }
            }
            catch { }
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }

    }
}
