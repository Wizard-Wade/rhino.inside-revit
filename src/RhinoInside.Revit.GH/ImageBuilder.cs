using System;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using Drawing = System.Drawing;

namespace RhinoInside.Revit.GH
{
  internal static class ImageBuilder
  {
    static void DrawIconTag(Drawing.Graphics g, Drawing.Brush textBrush, string tag, int width = 24, int height = 24)
    {
      g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
      g.InterpolationMode = Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
      g.PixelOffsetMode = Drawing.Drawing2D.PixelOffsetMode.HighQuality;

      var format = new Drawing.StringFormat()
      {
        Alignment = Drawing.StringAlignment.Center,
        LineAlignment = Drawing.StringAlignment.Center,
        Trimming = Drawing.StringTrimming.Character,
        FormatFlags = Drawing.StringFormatFlags.NoWrap
      };

      float emSize = ((float) (width) / ((float) tag.Length));
      if (width == 24)
      {
        switch (tag.Length)
        {
          case 1: emSize = 17.0f; break;
          case 2: emSize = 13.0f; break;
          case 3: emSize = 10.4f; break;
          case 4: emSize = 8.0f; break;
          default: emSize = 7.0f; break;
        }
      }

      // Avoid using ClearType rendering on icons that the user can zoom in like icons on Grashopper components.
      g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit;

      var rect = new Drawing.RectangleF(0.2f, 1.3f, width, height);
      using (var Calibri = new Drawing.Font("Calibri", emSize, Drawing.GraphicsUnit.Pixel))
        g.DrawString(tag, Calibri, textBrush, rect, format);
    }

    public static Drawing.Bitmap BuildIcon(string tag, int width = 24, int height = 24)
    {
      var bitmap = new Drawing.Bitmap(width, height);
      using (var g = Drawing.Graphics.FromImage(bitmap))
      {
        var iconBounds = new Drawing.RectangleF(0, 0, width, height);
        iconBounds.Inflate(-0.5f, -0.5f);

        using (var capsule = Grasshopper.GUI.Canvas.GH_Capsule.CreateCapsule(iconBounds, Grasshopper.GUI.Canvas.GH_Palette.Transparent))
          capsule.Render(g, false, false, false);

        DrawIconTag(g, Drawing.Brushes.Black, tag, width, height);
      }

      return bitmap;
    }

    public static Drawing.Bitmap BuildIcon(string tag, Guid componentId, int width = 24, int height = 24)
    {
      var bitmap = new Drawing.Bitmap(width, height);
      using (var g = Drawing.Graphics.FromImage(bitmap))
      {
        var textBrush = Drawing.Brushes.Black;
        if (Grasshopper.Instances.ComponentServer.EmitObjectIcon(componentId) is Drawing.Bitmap icon)
        {
          textBrush = Drawing.Brushes.White;
          g.DrawImage(icon, Drawing.Point.Empty);
        }
        else
        {
          var iconBounds = new Drawing.RectangleF(0, 0, width, height);
          iconBounds.Inflate(-0.5f, -0.5f);

          using (var capsule = Grasshopper.GUI.Canvas.GH_Capsule.CreateCapsule(iconBounds, Grasshopper.GUI.Canvas.GH_Palette.Transparent))
            capsule.Render(g, false, false, false);
        }

        DrawIconTag(g, textBrush, tag, width, height);
      }

      return bitmap;
    }

    public static Drawing.Bitmap BuildIcon(string tag, Drawing.Bitmap baseBitmap, int width = 24, int height = 24)
    {
      var bitmap = new Drawing.Bitmap(width, height);
      using (var g = Drawing.Graphics.FromImage(bitmap))
      {
        g.DrawImage(baseBitmap, new Drawing.Rectangle(0, 0, width, height));

        DrawIconTag(g, Drawing.Brushes.Black, tag, width, height);
      }

      return bitmap;
    }

    public static Drawing.Bitmap BuildImage(string tag, int width, int height, Drawing.Color color)
    {
      var bitmap = new Drawing.Bitmap(width, height);
      using (var g = Drawing.Graphics.FromImage(bitmap))
      {
        g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.InterpolationMode = Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        var rect = new Drawing.RectangleF(0.5f, 1.0f, width, height);

        var format = new Drawing.StringFormat()
        {
          Alignment = Drawing.StringAlignment.Center,
          LineAlignment = Drawing.StringAlignment.Center
        };

        if (color.IsEmpty)
          g.FillEllipse(Drawing.Brushes.Black, 1.0f, 1.0f, width - 2.0f, height - 2.0f);
        else using (var brush = new Drawing.SolidBrush(color))
            g.FillEllipse(brush, 1.0f, 1.0f, width - 2.0f, height - 2.0f);

        float emSize = ((float) (width) / ((float) tag.Length));
        if (width == 24)
        {
          switch (tag.Length)
          {
            case 1: emSize = 20.0f; break;
            case 2: emSize = 13.0f; break;
            case 3: emSize = 11.0f; break;
            case 4: emSize = 8.0f; break;
          }
        }

        // Avoid using ClearType rendering on icons that the user can zoom in like icons on Grashopper components.
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        using (var Calibri = new System.Drawing.Font("Calibri", emSize, Drawing.GraphicsUnit.Pixel))
          g.DrawString(tag, Calibri, Drawing.Brushes.White, rect, format);
      }

      return bitmap;
    }

    public static Drawing.Bitmap BuildOpacityMap(Drawing.Bitmap bitmap)
    {
      if (bitmap.PixelFormat == Drawing.Imaging.PixelFormat.Format32bppArgb)
      {
        var rectangle = new Drawing.Rectangle(Drawing.Point.Empty, bitmap.Size);
        var opacity = new Drawing.Bitmap(bitmap.Width, bitmap.Height, Drawing.Imaging.PixelFormat.Format8bppIndexed);
        var bitmapScanLine = new int[bitmap.Width];
        var opacityScanLine = new byte[bitmap.Width];

        var bitmapScanData = bitmap.LockBits(rectangle, Drawing.Imaging.ImageLockMode.WriteOnly, Drawing.Imaging.PixelFormat.Format32bppArgb);
        var opacityScanData = opacity?.LockBits(rectangle, Drawing.Imaging.ImageLockMode.WriteOnly, Drawing.Imaging.PixelFormat.Format8bppIndexed);

        bool isOpaque = true;
        for (int y = 0; y < rectangle.Height; ++y)
        {
          Marshal.Copy(bitmapScanData.Scan0 + (bitmapScanData.Stride * y), bitmapScanLine, 0, rectangle.Width);
          for (int x = 0; x < rectangle.Width; ++x)
          {
            var color = Drawing.Color.FromArgb(bitmapScanLine[x]);
            var colorA = color.A;
            if (colorA != byte.MaxValue) isOpaque = false;
            opacityScanLine[x] = colorA;
          }

          Marshal.Copy(opacityScanLine, 0, opacityScanData.Scan0 + (opacityScanData.Stride * y), rectangle.Width);
        }

        opacity.UnlockBits(opacityScanData);
        bitmap.UnlockBits(bitmapScanData);

        if (isOpaque)
        {
          opacity.Dispose();
          opacity = null;
        }
        else
        {
          var palette = opacity.Palette;
          for (int i = 0; i < byte.MaxValue; ++i)
            palette.Entries[i] = Drawing.Color.FromArgb(i, i, i);

          opacity.Palette = palette;
        }

        return opacity;
      }

      return null;
    }
  }
}
