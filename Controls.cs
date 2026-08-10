using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Portable2FA
{
    internal static class Palette
    {
        public static readonly Color Ink = Color.FromArgb(25, 35, 50);
        public static readonly Color Muted = Color.FromArgb(107, 119, 133);
        public static readonly Color Border = Color.FromArgb(218, 225, 232);
        public static readonly Color Surface = Color.White;
        public static readonly Color Canvas = Color.FromArgb(244, 247, 250);
        public static readonly Color Navy = Color.FromArgb(20, 34, 52);
        public static readonly Color Accent = Color.FromArgb(16, 156, 145);
        public static readonly Color AccentHover = Color.FromArgb(13, 136, 127);
        public static readonly Color Amber = Color.FromArgb(235, 166, 55);
        public static readonly Color Error = Color.FromArgb(194, 57, 52);
    }

    internal sealed class RoundedPanel : Panel
    {
        public int Radius { get; set; }
        public Color BorderColor { get; set; }

        public RoundedPanel()
        {
            Radius = 8;
            BorderColor = Palette.Border;
            DoubleBuffered = true;
            BackColor = Palette.Surface;
            ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = UiDrawing.RoundedRectangle(rect, Radius))
            using (SolidBrush brush = new SolidBrush(BackColor))
            using (Pen pen = new Pen(BorderColor))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            using (GraphicsPath path = UiDrawing.RoundedRectangle(
                new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height)), Radius))
            {
                Region = new Region(path);
            }
        }
    }

    internal sealed class ActionButton : Button
    {
        public Color NormalColor { get; set; }
        public Color HoverColor { get; set; }
        public int Radius { get; set; }

        public ActionButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseDownBackColor = Palette.AccentHover;
            FlatAppearance.MouseOverBackColor = Palette.AccentHover;
            UseVisualStyleBackColor = false;
            NormalColor = Palette.Accent;
            HoverColor = Palette.AccentHover;
            Radius = 7;
            BackColor = NormalColor;
            ForeColor = Color.White;
            Cursor = Cursors.Hand;
            TabStop = false;
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            BackColor = HoverColor;
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            BackColor = NormalColor;
            base.OnMouseLeave(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width <= 0 || Height <= 0)
                return;
            using (GraphicsPath path = UiDrawing.RoundedRectangle(
                new Rectangle(0, 0, Width, Height), Radius))
            {
                Region = new Region(path);
            }
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fill = Enabled ? BackColor : Color.FromArgb(221, 227, 232);
            Color content = Enabled ? ForeColor : Palette.Muted;
            pevent.Graphics.Clear(fill);
            using (GraphicsPath path = UiDrawing.RoundedRectangle(rect, Radius))
            using (SolidBrush brush = new SolidBrush(fill))
            {
                pevent.Graphics.FillPath(brush, path);
            }

            Rectangle iconRect = new Rectangle(18, Height / 2 - 9, 18, 18);
            using (Pen pen = new Pen(content, 1.8F))
            {
                pen.LineJoin = LineJoin.Round;
                pevent.Graphics.DrawRectangle(pen, iconRect.X + 5, iconRect.Y + 1, 10, 12);
                pevent.Graphics.DrawRectangle(pen, iconRect.X + 1, iconRect.Y + 5, 10, 12);
            }

            Rectangle textRect = new Rectangle(40, 0, Width - 52, Height);
            TextRenderer.DrawText(pevent.Graphics, Text, Font, textRect, content,
                TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter |
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }
    }

    internal sealed class UserAddButton : Button
    {
        private bool hovered;
        private bool pressed;

        public UserAddButton()
        {
            Text = "新增账户";
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            BackColor = Palette.Accent;
            ForeColor = Color.White;
            Cursor = Cursors.Hand;
            TabStop = false;
            Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            if (mevent.Button == MouseButtons.Left)
            {
                pressed = true;
                Invalidate();
            }
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            pressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Color fill = pressed ? Color.FromArgb(11, 119, 111) :
                (hovered ? Palette.AccentHover : Palette.Accent);
            pevent.Graphics.Clear(fill);
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            float scale = Math.Max(1F, Height / 32F);
            int centerY = Height / 2;
            int headLeft = (int)Math.Round(9F * scale);
            int headSize = (int)Math.Round(6F * scale);
            int bodyLeft = (int)Math.Round(6F * scale);
            int bodyWidth = (int)Math.Round(11F * scale);
            int bodyHeight = (int)Math.Round(10F * scale);
            int plusX = (int)Math.Round(23F * scale);
            int plusHalf = (int)Math.Round(3F * scale);
            int textLeft = (int)Math.Round(31F * scale);
            int textInset = (int)Math.Round(5F * scale);
            using (Pen pen = new Pen(Color.White, 1.6F * scale))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pevent.Graphics.DrawEllipse(pen, headLeft, centerY - (int)Math.Round(8F * scale),
                    headSize, headSize);
                pevent.Graphics.DrawArc(pen, bodyLeft, centerY - (int)Math.Round(1F * scale),
                    bodyWidth, bodyHeight, 200, 140);
                pevent.Graphics.DrawLine(pen, plusX, centerY - plusHalf, plusX,
                    centerY + plusHalf);
                pevent.Graphics.DrawLine(pen, plusX - plusHalf, centerY, plusX + plusHalf,
                    centerY);
            }

            Rectangle textBounds = new Rectangle(textLeft, 0,
                Math.Max(1, Width - textLeft - textInset), Height);
            TextRenderer.DrawText(pevent.Graphics, Text, Font, textBounds, Color.White,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left |
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }
    }

    internal sealed class IconButton : Button
    {
        public enum IconKind { Paste, Image, Crosshair, Eye, EyeOff }
        public IconKind Kind { get; set; }
        private bool hovered;

        public IconButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseDownBackColor = Color.FromArgb(226, 234, 240);
            FlatAppearance.MouseOverBackColor = Color.FromArgb(235, 241, 245);
            UseVisualStyleBackColor = false;
            BackColor = Color.White;
            Cursor = Cursors.Hand;
            TabStop = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            pevent.Graphics.Clear(hovered ? Color.FromArgb(235, 241, 245) : Color.White);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            OnPaintBackground(pevent);
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(Palette.Muted, 1.7F))
            {
                pen.LineJoin = LineJoin.Round;
                int cx = Width / 2;
                int cy = Height / 2;
                if (Kind == IconKind.Paste)
                {
                    pevent.Graphics.DrawRectangle(pen, cx - 7, cy - 7, 14, 16);
                    pevent.Graphics.DrawRectangle(pen, cx - 4, cy - 10, 8, 5);
                }
                else if (Kind == IconKind.Image)
                {
                    pevent.Graphics.DrawRectangle(pen, cx - 9, cy - 8, 18, 16);
                    pevent.Graphics.DrawEllipse(pen, cx + 3, cy - 5, 3, 3);
                    pevent.Graphics.DrawLines(pen, new Point[]
                    {
                        new Point(cx - 7, cy + 5),
                        new Point(cx - 2, cy),
                        new Point(cx + 1, cy + 3),
                        new Point(cx + 4, cy),
                        new Point(cx + 8, cy + 5)
                    });
                }
                else if (Kind == IconKind.Crosshair)
                {
                    pevent.Graphics.DrawEllipse(pen, cx - 7, cy - 7, 14, 14);
                    pevent.Graphics.DrawLine(pen, cx, cy - 10, cx, cy - 4);
                    pevent.Graphics.DrawLine(pen, cx, cy + 4, cx, cy + 10);
                    pevent.Graphics.DrawLine(pen, cx - 10, cy, cx - 4, cy);
                    pevent.Graphics.DrawLine(pen, cx + 4, cy, cx + 10, cy);
                }
                else
                {
                    using (GraphicsPath eye = new GraphicsPath())
                    {
                        eye.AddBezier(cx - 10, cy, cx - 5, cy - 7, cx + 5, cy - 7, cx + 10, cy);
                        eye.AddBezier(cx + 10, cy, cx + 5, cy + 7, cx - 5, cy + 7, cx - 10, cy);
                        pevent.Graphics.DrawPath(pen, eye);
                    }
                    pevent.Graphics.DrawEllipse(pen, cx - 2, cy - 2, 4, 4);
                    if (Kind == IconKind.EyeOff)
                        pevent.Graphics.DrawLine(pen, cx - 9, cy - 9, cx + 9, cy + 9);
                }
            }
        }
    }

    internal sealed class CountdownRing : Control
    {
        private double progress;
        private int seconds;

        public CountdownRing()
        {
            DoubleBuffered = true;
            Size = new Size(80, 80);
            progress = 1;
        }

        public void SetValue(int remainingSeconds, double remainingProgress)
        {
            seconds = remainingSeconds;
            progress = Math.Max(0, Math.Min(1, remainingProgress));
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int side = Math.Min(Width, Height) - 12;
            Rectangle rect = new Rectangle((Width - side) / 2, (Height - side) / 2, side, side);
            using (Pen track = new Pen(Color.FromArgb(228, 233, 238), 6F))
            using (Pen active = new Pen(progress <= 0.2 ? Palette.Amber : Palette.Accent, 6F))
            {
                track.StartCap = track.EndCap = LineCap.Round;
                active.StartCap = active.EndCap = LineCap.Round;
                e.Graphics.DrawArc(track, rect, -90, 360);
                if (progress > 0.001)
                    e.Graphics.DrawArc(active, rect, -90, (float)(360 * progress));
            }

            string value = seconds.ToString();
            using (Font numberFont = new Font("Segoe UI", 16F, FontStyle.Bold))
            {
                TextRenderer.DrawText(e.Graphics, value, numberFont, ClientRectangle, Palette.Ink,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            }
        }
    }

    internal static class UiDrawing
    {
        public static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = Math.Max(1, radius * 2);
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
