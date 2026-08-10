using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Portable2FA
{
    internal sealed class ScreenCaptureForm : Form
    {
        private readonly Bitmap desktop;
        private Point startPoint;
        private Rectangle selection;
        private bool selecting;

        public Bitmap CapturedImage { get; private set; }

        public ScreenCaptureForm()
        {
            Rectangle virtualScreen = SystemInformation.VirtualScreen;
            desktop = new Bitmap(virtualScreen.Width, virtualScreen.Height,
                PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(desktop))
            {
                graphics.CopyFromScreen(virtualScreen.Location, Point.Empty,
                    virtualScreen.Size, CopyPixelOperation.SourceCopy);
            }

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = virtualScreen.Location;
            Size = virtualScreen.Size;
            TopMost = true;
            ShowInTaskbar = false;
            Cursor = Cursors.Cross;
            KeyPreview = true;
            DoubleBuffered = true;
            BackColor = Color.Black;
            AutoScaleMode = AutoScaleMode.None;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.DrawImageUnscaled(desktop, Point.Empty);
            using (SolidBrush shade = new SolidBrush(Color.FromArgb(115, 0, 0, 0)))
                e.Graphics.FillRectangle(shade, ClientRectangle);

            if (selection.Width > 0 && selection.Height > 0)
            {
                e.Graphics.DrawImage(desktop, selection, selection, GraphicsUnit.Pixel);
                using (Pen border = new Pen(Color.FromArgb(24, 196, 177), 2F))
                {
                    border.DashStyle = DashStyle.Solid;
                    e.Graphics.DrawRectangle(border, selection);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            selecting = true;
            startPoint = e.Location;
            selection = Rectangle.Empty;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!selecting)
                return;
            selection = Normalize(startPoint, e.Location);
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (!selecting || e.Button != MouseButtons.Left)
                return;
            selecting = false;
            selection = Normalize(startPoint, e.Location);
            if (selection.Width < 8 || selection.Height < 8)
            {
                selection = Rectangle.Empty;
                Invalidate();
                return;
            }
            CapturedImage = desktop.Clone(selection, PixelFormat.Format32bppArgb);
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                desktop.Dispose();
            base.Dispose(disposing);
        }

        private static Rectangle Normalize(Point first, Point second)
        {
            int left = Math.Min(first.X, second.X);
            int top = Math.Min(first.Y, second.Y);
            int right = Math.Max(first.X, second.X);
            int bottom = Math.Max(first.Y, second.Y);
            return Rectangle.FromLTRB(left, top, right, bottom);
        }
    }
}
