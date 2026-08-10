using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Portable2FA
{
    internal sealed class MainForm : Form
    {
        private const int HotkeyId = 0x2FA;
        private const int CaptureHotkeyId = 0x2FB;
        private const int WmHotkey = 0x0312;
        private const uint ModAlt = 0x0001;
        private const uint ModControl = 0x0002;
        private const uint ModNoRepeat = 0x4000;

        private readonly TextBox secretInput;
        private readonly Label secretPlaceholder;
        private readonly Label codeLabel;
        private readonly Label accountLabel;
        private readonly Label detailLabel;
        private readonly Label statusLabel;
        private readonly Label emptyListLabel;
        private readonly Label syncStatusLabel;
        private readonly CountdownRing countdownRing;
        private readonly ActionButton copyButton;
        private readonly Button saveAccountButton;
        private readonly IconButton visibilityButton;
        private readonly ListBox accountList;
        private readonly Timer refreshTimer;
        private readonly NotifyIcon trayIcon;
        private readonly ToolStripMenuItem trayCopyItem;
        private readonly ToolTip toolTip;
        private readonly bool startHidden;

        private TotpProfile currentProfile;
        private string currentCode;
        private string selectedAccountId;
        private List<SavedAccount> savedAccounts;
        private AppSettings settings;
        private bool exiting;
        private bool trayHintShown;
        private bool hotkeyRegistered;
        private bool captureHotkeyRegistered;
        private bool loadingAccount;
        private DateTime copyFeedbackUntil;
        private DateTime nextSyncPoll;
        private int lastAccountTooltipIndex = -1;

        public MainForm(bool startHiddenValue)
        {
            startHidden = startHiddenValue;
            settings = AppSettings.Load();
            savedAccounts = new List<SavedAccount>();

            Text = "Portable 2FA";
            ClientSize = new Size(760, 526);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Palette.Canvas;
            Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            KeyPreview = true;

            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { }

            Controls.Add(BuildHeader());

            Panel sidebar = new Panel();
            sidebar.Location = new Point(0, 88);
            sidebar.Size = new Size(240, 438);
            sidebar.BackColor = Color.White;
            Controls.Add(sidebar);

            Label accountsTitle = NewLabel("账户库", 18, 15, 90, 30, 13F,
                FontStyle.Bold, Palette.Ink);
            sidebar.Controls.Add(accountsTitle);

            UserAddButton addAccount = new UserAddButton();
            addAccount.Location = new Point(122, 13);
            addAccount.Size = new Size(100, 32);
            addAccount.Click += delegate { StartNewAccount(); };
            sidebar.Controls.Add(addAccount);

            accountList = new ListBox();
            accountList.Location = new Point(10, 54);
            accountList.Size = new Size(220, 292);
            accountList.BorderStyle = BorderStyle.None;
            accountList.BackColor = Color.White;
            accountList.DrawMode = DrawMode.OwnerDrawFixed;
            accountList.ItemHeight = 56;
            accountList.IntegralHeight = false;
            accountList.DrawItem += DrawAccountItem;
            accountList.SelectedIndexChanged += AccountSelected;
            accountList.MouseMove += AccountListMouseMove;
            accountList.MouseLeave += delegate { lastAccountTooltipIndex = -1; };
            sidebar.Controls.Add(accountList);

            emptyListLabel = NewLabel("还没有保存账户", 20, 165, 200, 30, 9F,
                FontStyle.Regular, Palette.Muted);
            emptyListLabel.TextAlign = ContentAlignment.MiddleCenter;
            emptyListLabel.Cursor = Cursors.Hand;
            emptyListLabel.Click += delegate { StartNewAccount(); };
            sidebar.Controls.Add(emptyListLabel);
            emptyListLabel.BringToFront();

            ContextMenuStrip accountMenu = new ContextMenuStrip();
            accountMenu.Font = new Font("Microsoft YaHei UI", 9F);
            ToolStripMenuItem editAccountItem = new ToolStripMenuItem("编辑账户");
            editAccountItem.Click += delegate { SaveCurrentAccount(); };
            ToolStripMenuItem deleteAccountItem = new ToolStripMenuItem("删除账户");
            deleteAccountItem.Click += DeleteSelectedAccount;
            accountMenu.Items.Add(editAccountItem);
            accountMenu.Items.Add(deleteAccountItem);
            accountList.ContextMenuStrip = accountMenu;

            syncStatusLabel = NewLabel("本机加密保存", 18, 353, 204, 22, 8.5F,
                FontStyle.Regular, Palette.Muted);
            sidebar.Controls.Add(syncStatusLabel);

            Button settingsButton = NewFlatButton("设置", 18, 385, 204, 36, false);
            settingsButton.Click += OpenSettings;
            sidebar.Controls.Add(settingsButton);

            Label inputTitle = NewLabel("身份验证密钥", 264, 108, 180, 24, 10F,
                FontStyle.Bold, Palette.Ink);
            Controls.Add(inputTitle);

            RoundedPanel inputSurface = new RoundedPanel();
            inputSurface.Location = new Point(264, 137);
            inputSurface.Size = new Size(472, 48);
            Controls.Add(inputSurface);

            secretInput = new TextBox();
            secretInput.BorderStyle = BorderStyle.None;
            secretInput.Font = new Font("Segoe UI", 11F);
            secretInput.Location = new Point(14, 13);
            secretInput.Size = new Size(290, 25);
            secretInput.UseSystemPasswordChar = true;
            secretInput.BackColor = Color.White;
            secretInput.TextChanged += delegate
            {
                UpdateSecretPlaceholder();
                ParseInput();
            };
            secretInput.KeyDown += SecretInputKeyDown;
            inputSurface.Controls.Add(secretInput);

            secretPlaceholder = NewLabel("粘贴密钥、otpauth:// 或二维码",
                14, 11, 286, 26, 9.5F, FontStyle.Regular, Color.FromArgb(137, 147, 158));
            secretPlaceholder.Cursor = Cursors.IBeam;
            secretPlaceholder.Click += delegate { secretInput.Focus(); };
            inputSurface.Controls.Add(secretPlaceholder);
            secretPlaceholder.BringToFront();

            IconButton screenButton = new IconButton();
            screenButton.Kind = IconButton.IconKind.Crosshair;
            screenButton.Location = new Point(311, 5);
            screenButton.Size = new Size(36, 38);
            screenButton.Click += delegate { StartScreenQrCapture(); };
            inputSurface.Controls.Add(screenButton);

            visibilityButton = new IconButton();
            visibilityButton.Kind = IconButton.IconKind.Eye;
            visibilityButton.Location = new Point(350, 5);
            visibilityButton.Size = new Size(36, 38);
            visibilityButton.Click += ToggleSecretVisibility;
            inputSurface.Controls.Add(visibilityButton);

            IconButton qrFileButton = new IconButton();
            qrFileButton.Kind = IconButton.IconKind.Image;
            qrFileButton.Location = new Point(389, 5);
            qrFileButton.Size = new Size(36, 38);
            qrFileButton.Click += ChooseQrImage;
            inputSurface.Controls.Add(qrFileButton);

            IconButton pasteButton = new IconButton();
            pasteButton.Kind = IconButton.IconKind.Paste;
            pasteButton.Location = new Point(428, 5);
            pasteButton.Size = new Size(36, 38);
            pasteButton.Click += PasteSecret;
            inputSurface.Controls.Add(pasteButton);

            toolTip = new ToolTip();
            toolTip.SetToolTip(screenButton, "截图识别二维码");
            toolTip.SetToolTip(visibilityButton, "显示密钥");
            toolTip.SetToolTip(qrFileButton, "选择二维码图片");
            toolTip.SetToolTip(pasteButton, "粘贴密钥或二维码图片");

            RoundedPanel codeSurface = new RoundedPanel();
            codeSurface.Location = new Point(264, 201);
            codeSurface.Size = new Size(472, 244);
            Controls.Add(codeSurface);

            codeSurface.Controls.Add(NewLabel("动态验证码", 20, 18, 150, 24, 10F,
                FontStyle.Bold, Palette.Ink));

            accountLabel = NewLabel("", 20, 43, 322, 20, 8.5F,
                FontStyle.Regular, Palette.Muted);
            accountLabel.AutoEllipsis = true;
            codeSurface.Controls.Add(accountLabel);

            codeLabel = NewLabel("--- ---", 20, 70, 334, 66, 40F,
                FontStyle.Bold, Palette.Ink);
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

            detailLabel = NewLabel("SHA1  ·  6 位  ·  30 秒", 20, 139, 250, 22,
                8.5F, FontStyle.Regular, Palette.Muted);
            codeSurface.Controls.Add(detailLabel);

            copyButton = new ActionButton();
            copyButton.Text = "复制验证码";
            copyButton.Location = new Point(20, 179);
            copyButton.Size = new Size(190, 46);
            copyButton.Enabled = false;
            copyButton.Click += delegate { CopyCurrentCode(); };
            codeSurface.Controls.Add(copyButton);

            saveAccountButton = NewFlatButton("保存账户", 224, 184, 108, 36, false);
            saveAccountButton.Enabled = false;
            saveAccountButton.Click += delegate { SaveCurrentAccount(); };
            codeSurface.Controls.Add(saveAccountButton);

            Label shortcut = NewLabel("点击验证码复制", 340, 190, 112, 24, 8F,
                FontStyle.Regular, Palette.Muted);
            shortcut.TextAlign = ContentAlignment.MiddleRight;
            codeSurface.Controls.Add(shortcut);

            statusLabel = NewLabel("等待输入密钥", 264, 463, 472, 25, 9F,
                FontStyle.Regular, Palette.Muted);
            Controls.Add(statusLabel);

            Label privacyLabel = NewLabel("保存的密钥由 Windows 加密保护", 264, 493,
                472, 18, 8F, FontStyle.Regular, Color.FromArgb(136, 146, 158));
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
            ToolStripMenuItem settingsItem = new ToolStripMenuItem("设置");
            settingsItem.Click += OpenSettings;
            ToolStripMenuItem captureItem = new ToolStripMenuItem("截图识别二维码");
            captureItem.Click += delegate { StartScreenQrCapture(); };
            ToolStripMenuItem exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += ExitApplication;
            trayMenu.Items.Add(showItem);
            trayMenu.Items.Add(trayCopyItem);
            trayMenu.Items.Add(captureItem);
            trayMenu.Items.Add(settingsItem);
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
            Shown += MainFormShown;
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
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyHotkeySetting(false);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (hotkeyRegistered)
                UnregisterHotKey(Handle, HotkeyId);
            if (captureHotkeyRegistered)
                UnregisterHotKey(Handle, CaptureHotkeyId);
            hotkeyRegistered = false;
            captureHotkeyRegistered = false;
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmHotkey && m.WParam.ToInt32() == HotkeyId)
            {
                RestoreFromTray();
                return;
            }
            if (m.Msg == WmHotkey && m.WParam.ToInt32() == CaptureHotkeyId)
            {
                BeginInvoke(new Action(StartScreenQrCapture));
                return;
            }
            base.WndProc(ref m);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.C) && !secretInput.Focused &&
                currentProfile != null)
            {
                CopyCurrentCode();
                return true;
            }
            if (keyData == Keys.Escape)
            {
                HideToTray(true);
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

            header.Controls.Add(NewLabel("Portable 2FA", 84, 19, 300, 32, 18F,
                FontStyle.Bold, Color.White));
            header.Controls.Add(NewLabel("快速、安全的动态验证码", 85, 51, 300, 22, 9F,
                FontStyle.Regular, Color.FromArgb(181, 196, 211)));

            Label version = NewLabel("v" + BuildInfo.Version, 646, 25, 88, 20,
                8.5F, FontStyle.Bold, Color.FromArgb(221, 230, 238));
            version.TextAlign = ContentAlignment.MiddleRight;
            header.Controls.Add(version);

            string updatedAt = BuildInfo.UpdatedAt;
            if (updatedAt.Length >= 16)
                updatedAt = updatedAt.Substring(0, 16).Replace('T', ' ');
            Label updated = NewLabel("更新 " + updatedAt, 594, 48, 140, 19,
                7.5F, FontStyle.Regular, Color.FromArgb(152, 171, 189));
            updated.TextAlign = ContentAlignment.MiddleRight;
            header.Controls.Add(updated);
            return header;
        }

        private void MainFormShown(object sender, EventArgs e)
        {
            ApplyHotkeySetting(false);
            LoadAccounts();
            UpdateSecretPlaceholder();
            if (startHidden)
                BeginInvoke(new Action(delegate { HideToTray(false); }));
        }

        private void LoadAccounts()
        {
            try
            {
                savedAccounts = VaultStore.Load();
                if (settings.WindowsSync)
                    SynchronizeWindows(false);
                RefreshAccountList(null);
                if (accountList.Items.Count > 0)
                    accountList.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                savedAccounts = new List<SavedAccount>();
                RefreshAccountList(null);
                statusLabel.Text = "账户库读取失败：" + ex.Message;
                statusLabel.ForeColor = Palette.Error;
            }
            UpdateSyncStatus();
        }

        private void RefreshAccountList(string selectId)
        {
            loadingAccount = true;
            accountList.BeginUpdate();
            accountList.Items.Clear();
            List<SavedAccount> active = ActiveAccounts();
            active.Sort(delegate(SavedAccount left, SavedAccount right)
            {
                return string.Compare(left.DisplayName, right.DisplayName,
                    StringComparison.CurrentCultureIgnoreCase);
            });
            foreach (SavedAccount account in active)
                accountList.Items.Add(account);
            accountList.EndUpdate();
            emptyListLabel.Visible = accountList.Items.Count == 0;

            string target = string.IsNullOrWhiteSpace(selectId) ? selectedAccountId : selectId;
            if (!string.IsNullOrWhiteSpace(target))
            {
                for (int i = 0; i < accountList.Items.Count; i++)
                {
                    SavedAccount item = (SavedAccount)accountList.Items[i];
                    if (item.Id.Equals(target, StringComparison.OrdinalIgnoreCase))
                    {
                        accountList.SelectedIndex = i;
                        break;
                    }
                }
            }
            loadingAccount = false;
        }

        private List<SavedAccount> ActiveAccounts()
        {
            List<SavedAccount> values = new List<SavedAccount>();
            foreach (SavedAccount account in savedAccounts)
            {
                if (!account.Deleted)
                    values.Add(account);
            }
            return values;
        }

        private void DrawAccountItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= accountList.Items.Count)
                return;
            SavedAccount account = (SavedAccount)accountList.Items[e.Index];
            bool selected = (e.State & DrawItemState.Selected) != 0;
            Color background = selected ? Color.FromArgb(230, 247, 244) : Color.White;
            using (SolidBrush brush = new SolidBrush(background))
                e.Graphics.FillRectangle(brush, e.Bounds);
            if (selected)
            {
                using (SolidBrush accent = new SolidBrush(Palette.Accent))
                    e.Graphics.FillRectangle(accent, e.Bounds.X, e.Bounds.Y + 7, 3,
                        e.Bounds.Height - 14);
            }

            Rectangle titleBounds = new Rectangle(e.Bounds.X + 12, e.Bounds.Y + 8,
                e.Bounds.Width - 20, 21);
            Rectangle subtitleBounds = new Rectangle(e.Bounds.X + 12, e.Bounds.Y + 31,
                e.Bounds.Width - 20, 18);
            float titleSize = 9.5F;
            using (Font measureFont = new Font("Microsoft YaHei UI", titleSize, FontStyle.Bold))
            {
                Size measured = TextRenderer.MeasureText(e.Graphics, account.DisplayName,
                    measureFont, titleBounds.Size, TextFormatFlags.NoPadding);
                if (measured.Width > titleBounds.Width)
                    titleSize = 8.5F;
            }
            using (Font titleFont = new Font("Microsoft YaHei UI", titleSize, FontStyle.Bold))
            using (Font subtitleFont = new Font("Microsoft YaHei UI", 8F))
            {
                TextRenderer.DrawText(e.Graphics, account.DisplayName, titleFont, titleBounds,
                    Palette.Ink, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                TextRenderer.DrawText(e.Graphics, account.Subtitle, subtitleFont, subtitleBounds,
                    Palette.Muted, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }
        }

        private void AccountListMouseMove(object sender, MouseEventArgs e)
        {
            int index = accountList.IndexFromPoint(e.Location);
            if (index == lastAccountTooltipIndex)
                return;
            lastAccountTooltipIndex = index;
            if (index < 0 || index >= accountList.Items.Count)
            {
                toolTip.SetToolTip(accountList, string.Empty);
                return;
            }
            SavedAccount account = (SavedAccount)accountList.Items[index];
            string details = account.DisplayName;
            if (!string.IsNullOrWhiteSpace(account.Subtitle))
                details += Environment.NewLine + account.Subtitle;
            toolTip.SetToolTip(accountList, details);
        }

        private void AccountSelected(object sender, EventArgs e)
        {
            if (loadingAccount || accountList.SelectedItem == null)
                return;
            SavedAccount account = (SavedAccount)accountList.SelectedItem;
            selectedAccountId = account.Id;
            loadingAccount = true;
            secretInput.Text = account.ToOtpAuthUri();
            secretInput.SelectionStart = secretInput.TextLength;
            loadingAccount = false;
            saveAccountButton.Text = "更新账户";
            toolTip.SetToolTip(accountLabel, account.DisplayName + Environment.NewLine +
                account.Subtitle);
        }

        private void StartNewAccount()
        {
            selectedAccountId = null;
            loadingAccount = true;
            accountList.ClearSelected();
            secretInput.Clear();
            loadingAccount = false;
            saveAccountButton.Text = "保存账户";
            secretInput.Focus();
        }

        private void SaveCurrentAccount()
        {
            if (currentProfile == null)
                return;
            SavedAccount existing = FindAccount(selectedAccountId);
            string issuer = currentProfile.Issuer;
            string account = currentProfile.AccountLabel;
            string label = string.IsNullOrWhiteSpace(issuer) ? account : issuer;
            int separator = account.IndexOf(':');
            if (separator >= 0)
                account = account.Substring(separator + 1).Trim();
            if (existing != null)
            {
                label = existing.Label;
                issuer = existing.Issuer;
                account = existing.Account;
            }

            using (AccountDialog dialog = new AccountDialog(label, issuer, account,
                existing != null))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                SavedAccount value = SavedAccount.FromProfile(currentProfile,
                    dialog.LabelValue, dialog.IssuerValue, dialog.AccountValue,
                    existing == null ? null : existing.Id);
                if (existing != null)
                    savedAccounts.Remove(existing);
                savedAccounts.Add(value);
                selectedAccountId = value.Id;
                if (PersistAccounts("账户已安全保存"))
                {
                    RefreshAccountList(value.Id);
                    loadingAccount = true;
                    secretInput.Text = value.ToOtpAuthUri();
                    loadingAccount = false;
                    saveAccountButton.Text = "更新账户";
                }
            }
        }

        private void DeleteSelectedAccount(object sender, EventArgs e)
        {
            SavedAccount account = FindAccount(selectedAccountId);
            if (account == null)
                return;
            DialogResult answer = MessageBox.Show(this,
                "确定删除“" + account.DisplayName + "”吗？", "删除账户",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes)
                return;

            account.Deleted = true;
            account.UpdatedAt = DateTime.UtcNow.ToString("o");
            if (PersistAccounts("账户已删除"))
            {
                StartNewAccount();
                RefreshAccountList(null);
                if (accountList.Items.Count > 0)
                    accountList.SelectedIndex = 0;
            }
        }

        private SavedAccount FindAccount(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;
            foreach (SavedAccount account in savedAccounts)
            {
                if (account.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                    return account;
            }
            return null;
        }

        private bool PersistAccounts(string successMessage)
        {
            try
            {
                VaultStore.Save(savedAccounts);
                if (settings.WindowsSync)
                    WindowsCredentialSync.Push(savedAccounts);
                statusLabel.Text = settings.WindowsSync
                    ? successMessage + "并同步到 Windows"
                    : successMessage;
                statusLabel.ForeColor = Palette.Accent;
                return true;
            }
            catch (Exception ex)
            {
                statusLabel.Text = "保存失败：" + ex.Message;
                statusLabel.ForeColor = Palette.Error;
                return false;
            }
        }

        private void OpenSettings(object sender, EventArgs e)
        {
            bool wasSyncEnabled = settings.WindowsSync;
            using (SettingsForm dialog = new SettingsForm(settings))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                settings = dialog.ResultSettings;
            }

            ApplyHotkeySetting(true);
            if (settings.WindowsSync)
            {
                try
                {
                    if (!wasSyncEnabled)
                        SynchronizeWindows(true);
                    else
                        WindowsCredentialSync.Push(savedAccounts);
                }
                catch (Exception ex)
                {
                    settings.WindowsSync = false;
                    settings.Save();
                    statusLabel.Text = "Windows 同步未启用：" + ex.Message;
                    statusLabel.ForeColor = Palette.Error;
                }
            }
            UpdateSyncStatus();
        }

        private void SynchronizeWindows(bool showMessage)
        {
            List<SavedAccount> remote = WindowsCredentialSync.Pull();
            savedAccounts = SavedAccount.Merge(savedAccounts, remote);
            WindowsCredentialSync.Push(savedAccounts);
            VaultStore.Save(savedAccounts);
            RefreshAccountList(selectedAccountId);
            if (showMessage)
            {
                statusLabel.Text = "Windows 账户同步已启用";
                statusLabel.ForeColor = Palette.Accent;
            }
            nextSyncPoll = DateTime.UtcNow.AddSeconds(15);
        }

        private void UpdateSyncStatus()
        {
            syncStatusLabel.Text = settings.WindowsSync
                ? "Windows 同步  ·  最多 20 个"
                : "本机 DPAPI 加密保存";
            syncStatusLabel.ForeColor = settings.WindowsSync ? Palette.Accent : Palette.Muted;
        }

        private void ApplyHotkeySetting(bool report)
        {
            if (!IsHandleCreated)
                return;
            if (hotkeyRegistered)
            {
                UnregisterHotKey(Handle, HotkeyId);
                hotkeyRegistered = false;
            }
            if (captureHotkeyRegistered)
            {
                UnregisterHotKey(Handle, CaptureHotkeyId);
                captureHotkeyRegistered = false;
            }
            if (!settings.EnableHotkey)
                return;
            hotkeyRegistered = RegisterHotKey(Handle, HotkeyId,
                ToNativeModifiers((Keys)settings.ShowHotkeyModifiers) | ModNoRepeat,
                (uint)settings.ShowHotkeyKey);
            captureHotkeyRegistered = RegisterHotKey(Handle, CaptureHotkeyId,
                ToNativeModifiers((Keys)settings.CaptureHotkeyModifiers) | ModNoRepeat,
                (uint)settings.CaptureHotkeyKey);
            if (report)
            {
                bool registered = hotkeyRegistered && captureHotkeyRegistered;
                statusLabel.Text = registered
                    ? "全局快捷键已更新"
                    : "一个或多个快捷键已被其他程序占用";
                statusLabel.ForeColor = registered ? Palette.Accent : Palette.Error;
            }
        }

        private static uint ToNativeModifiers(Keys modifiers)
        {
            uint value = 0;
            if ((modifiers & Keys.Control) == Keys.Control) value |= ModControl;
            if ((modifiers & Keys.Alt) == Keys.Alt) value |= ModAlt;
            if ((modifiers & Keys.Shift) == Keys.Shift) value |= 0x0004;
            return value;
        }

        private void StartScreenQrCapture()
        {
            bool wasVisible = Visible;
            HideToTray(false);
            Bitmap captured = null;
            try
            {
                using (ScreenCaptureForm capture = new ScreenCaptureForm())
                {
                    if (capture.ShowDialog() == DialogResult.OK)
                        captured = capture.CapturedImage;
                }
                if (captured == null)
                {
                    if (wasVisible) RestoreFromTray();
                    return;
                }

                string value = QrCodeDecoder.Decode(captured);
                RestoreFromTray();
                StartNewAccount();
                secretInput.Text = value;
                secretInput.SelectionStart = secretInput.TextLength;
                statusLabel.Text = "已从屏幕截图导入二维码";
                statusLabel.ForeColor = Palette.Accent;
            }
            catch (FormatException ex)
            {
                RestoreFromTray();
                SetStatusError(ex.Message);
            }
            finally
            {
                if (captured != null)
                    captured.Dispose();
            }
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
                saveAccountButton.Enabled = true;
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
            if (secretPlaceholder != null)
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
            saveAccountButton.Enabled = false;
            trayCopyItem.Enabled = false;
        }

        private void RefreshTick(object sender, EventArgs e)
        {
            if (currentProfile != null)
                UpdateCode();

            if (copyFeedbackUntil != DateTime.MinValue &&
                DateTime.UtcNow >= copyFeedbackUntil)
            {
                copyFeedbackUntil = DateTime.MinValue;
                copyButton.Text = "复制验证码";
                copyButton.Invalidate();
            }

            if (settings.WindowsSync && DateTime.UtcNow >= nextSyncPoll)
            {
                nextSyncPoll = DateTime.UtcNow.AddSeconds(15);
                try { SynchronizeWindows(false); }
                catch { }
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

            double elapsed = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0,
                DateTimeKind.Utc)).TotalSeconds;
            double intoPeriod = elapsed % currentProfile.Period;
            double remaining = currentProfile.Period - intoPeriod;
            countdownRing.SetValue(Math.Max(1, (int)Math.Ceiling(remaining)),
                remaining / currentProfile.Period);
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
                if (Clipboard.ContainsImage())
                {
                    using (Image image = Clipboard.GetImage())
                        ImportQrImage(image, "已从剪贴板二维码导入密钥");
                    return;
                }
                if (Clipboard.ContainsText())
                {
                    secretInput.Text = Clipboard.GetText().Trim();
                    secretInput.SelectionStart = secretInput.TextLength;
                    secretInput.Focus();
                    return;
                }
                SetStatusError("剪贴板中没有文本或二维码图片");
            }
            catch (ExternalException) { SetStatusError("剪贴板正被其他程序占用，请再试一次"); }
            catch (FormatException ex) { SetStatusError(ex.Message); }
            catch (ArgumentException) { SetStatusError("剪贴板图片格式无效"); }
        }

        private void ChooseQrImage(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "选择 2FA 二维码图片";
                dialog.Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|所有文件|*.*";
                dialog.CheckFileExists = true;
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                try
                {
                    using (Image image = Image.FromFile(dialog.FileName))
                        ImportQrImage(image, "已从二维码图片导入密钥");
                }
                catch (FormatException ex) { SetStatusError(ex.Message); }
                catch (OutOfMemoryException) { SetStatusError("所选文件不是有效图片或图片过大"); }
                catch (IOException) { SetStatusError("无法读取所选图片"); }
                catch (UnauthorizedAccessException) { SetStatusError("没有权限读取所选图片"); }
            }
        }

        private void ImportQrImage(Image image, string successMessage)
        {
            if (image == null)
                throw new FormatException("图片内容为空");
            using (Bitmap bitmap = new Bitmap(image))
            {
                StartNewAccount();
                secretInput.Text = QrCodeDecoder.Decode(bitmap);
                secretInput.SelectionStart = secretInput.TextLength;
                secretInput.Focus();
                statusLabel.Text = successMessage;
                statusLabel.ForeColor = Palette.Accent;
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
                ? IconButton.IconKind.Eye : IconButton.IconKind.EyeOff;
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
            HideToTray(true);
        }

        private void MinimizeToTray(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
                HideToTray(false);
        }

        private void HideToTray(bool showHint)
        {
            Hide();
            ShowInTaskbar = false;
            if (showHint && !trayHintShown)
            {
                trayHintShown = true;
                trayIcon.BalloonTipTitle = "Portable 2FA 仍在运行";
                trayIcon.BalloonTipText = "按 Ctrl+Alt+T 或双击托盘图标可恢复";
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

        private void SetStatusError(string message)
        {
            statusLabel.Text = message;
            statusLabel.ForeColor = Palette.Error;
        }

        private static Button NewFlatButton(string text, int x, int y, int width,
            int height, bool primary)
        {
            Button button = new Button();
            button.Text = text;
            button.Location = new Point(x, y);
            button.Size = new Size(width, height);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = Palette.Border;
            button.BackColor = primary ? Palette.Accent : Color.White;
            button.ForeColor = primary ? Color.White : Palette.Ink;
            button.Font = new Font("Microsoft YaHei UI", 9F,
                primary ? FontStyle.Bold : FontStyle.Regular);
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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id,
            uint modifiers, uint virtualKey);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
