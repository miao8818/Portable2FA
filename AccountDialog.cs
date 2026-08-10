using System;
using System.Drawing;
using System.Windows.Forms;

namespace Portable2FA
{
    internal sealed class AccountDialog : Form
    {
        private readonly TextBox issuerInput;
        private readonly TextBox accountInput;
        private readonly TextBox labelInput;

        public string LabelValue { get { return labelInput.Text.Trim(); } }
        public string IssuerValue { get { return issuerInput.Text.Trim(); } }
        public string AccountValue { get { return accountInput.Text.Trim(); } }

        public AccountDialog(string label, string issuer, string account, bool editing)
        {
            Text = editing ? "编辑账户" : "保存账户";
            ClientSize = new Size(400, 310);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.White;
            Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;

            Label title = NewLabel(editing ? "编辑账户信息" : "保存到加密账户库",
                24, 18, 340, 30, 14F, FontStyle.Bold, Palette.Ink);
            Controls.Add(title);

            Controls.Add(NewLabel("标签", 24, 63, 120, 22, 9F,
                FontStyle.Bold, Palette.Ink));
            labelInput = NewTextBox(24, 88, 352);
            labelInput.MaxLength = 160;
            labelInput.Text = label ?? string.Empty;
            Controls.Add(labelInput);

            Controls.Add(NewLabel("服务名称", 24, 126, 120, 22, 9F,
                FontStyle.Bold, Palette.Ink));
            issuerInput = NewTextBox(24, 88, 352);
            issuerInput.Location = new Point(24, 151);
            issuerInput.Text = issuer ?? string.Empty;
            Controls.Add(issuerInput);

            Controls.Add(NewLabel("账户", 24, 189, 120, 22, 9F,
                FontStyle.Bold, Palette.Ink));
            accountInput = NewTextBox(24, 214, 352);
            accountInput.MaxLength = 320;
            accountInput.Text = account ?? string.Empty;
            Controls.Add(accountInput);

            Button cancel = NewButton("取消", 216, 263, 76, false);
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);

            Button save = NewButton(editing ? "保存修改" : "保存", 300, 263, 76, true);
            save.Click += SaveClick;
            Controls.Add(save);

            AcceptButton = save;
            CancelButton = cancel;
        }

        private void SaveClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(labelInput.Text))
            {
                MessageBox.Show(this, "请填写用于识别该密钥的标签。", "账户标签",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                labelInput.Focus();
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private static TextBox NewTextBox(int x, int y, int width)
        {
            TextBox input = new TextBox();
            input.Location = new Point(x, y);
            input.Size = new Size(width, 28);
            input.Font = new Font("Microsoft YaHei UI", 10F);
            return input;
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
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }
    }
}
