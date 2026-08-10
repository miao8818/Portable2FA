using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

internal static class IconMaker
{
    private static readonly int[] Sizes = { 16, 20, 24, 32, 40, 48, 64, 128, 256 };

    private static void Main(string[] args)
    {
        string output = args.Length > 0 ? args[0] : ".";
        Directory.CreateDirectory(output);

        WriteIcon(Path.Combine(output, "app.ico"), false);
        WriteIcon(Path.Combine(output, "tray.ico"), true);
        using (Bitmap preview = DrawIcon(512, false))
            preview.Save(Path.Combine(output, "app-preview.png"), ImageFormat.Png);
    }

    private static void WriteIcon(string path, bool trayStyle)
    {
        List<byte[]> images = new List<byte[]>();
        foreach (int size in Sizes)
        {
            using (Bitmap bitmap = DrawIcon(size, trayStyle))
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                images.Add(stream.ToArray());
            }
        }

        using (FileStream file = File.Create(path))
        using (BinaryWriter writer = new BinaryWriter(file))
        {
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)images.Count);

            int offset = 6 + images.Count * 16;
            for (int i = 0; i < images.Count; i++)
            {
                int size = Sizes[i];
                writer.Write((byte)(size >= 256 ? 0 : size));
                writer.Write((byte)(size >= 256 ? 0 : size));
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(images[i].Length);
                writer.Write(offset);
                offset += images[i].Length;
            }

            foreach (byte[] image in images)
                writer.Write(image);
        }
    }

    private static Bitmap DrawIcon(int size, bool trayStyle)
    {
        Bitmap bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            float scale = size / 256F;
            RectangleF tile = new RectangleF(10 * scale, 10 * scale, 236 * scale, 236 * scale);
            using (GraphicsPath tilePath = RoundedRect(tile, 48 * scale))
            using (LinearGradientBrush tileBrush = new LinearGradientBrush(
                tile, Color.FromArgb(18, 31, 49), Color.FromArgb(28, 53, 70), 35F))
            {
                g.FillPath(tileBrush, tilePath);
            }

            if (!trayStyle || size >= 32)
            {
                using (Pen arcPen = new Pen(Color.FromArgb(29, 196, 177), 15 * scale))
                {
                    arcPen.StartCap = arcPen.EndCap = LineCap.Round;
                    g.DrawArc(arcPen, 44 * scale, 43 * scale, 168 * scale, 168 * scale, -72, 238);
                }
                using (SolidBrush dot = new SolidBrush(Color.FromArgb(239, 173, 62)))
                    g.FillEllipse(dot, 42 * scale, 177 * scale, 24 * scale, 24 * scale);
            }

            using (GraphicsPath shield = new GraphicsPath())
            {
                shield.AddLine(128 * scale, 56 * scale, 185 * scale, 78 * scale);
                shield.AddLine(185 * scale, 78 * scale, 179 * scale, 144 * scale);
                shield.AddBezier(179 * scale, 144 * scale, 173 * scale, 177 * scale,
                    151 * scale, 195 * scale, 128 * scale, 207 * scale);
                shield.AddBezier(128 * scale, 207 * scale, 105 * scale, 195 * scale,
                    83 * scale, 177 * scale, 77 * scale, 144 * scale);
                shield.AddLine(77 * scale, 144 * scale, 71 * scale, 78 * scale);
                shield.CloseFigure();
                using (SolidBrush white = new SolidBrush(Color.White))
                    g.FillPath(white, shield);
            }

            using (SolidBrush navy = new SolidBrush(Color.FromArgb(20, 37, 54)))
            {
                g.FillEllipse(navy, 111 * scale, 101 * scale, 34 * scale, 34 * scale);
                using (GraphicsPath stem = RoundedRect(
                    new RectangleF(119 * scale, 124 * scale, 18 * scale, 38 * scale), 8 * scale))
                    g.FillPath(navy, stem);
            }
        }
        return bitmap;
    }

    private static GraphicsPath RoundedRect(RectangleF bounds, float radius)
    {
        GraphicsPath path = new GraphicsPath();
        float d = radius * 2;
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
