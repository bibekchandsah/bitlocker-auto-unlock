using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace BitLockerManager
{
    public static class IconHelper
    {
        public static Icon CreateIconFromPng(string pngPath)
        {
            try
            {
                // Try to load ICO file first (better quality)
                var icoPath = pngPath.Replace(".png", ".ico");
                if (File.Exists(icoPath))
                {
                    return new Icon(icoPath);
                }
                
                // Fallback to PNG
                if (File.Exists(pngPath))
                {
                    using (var bitmap = new Bitmap(pngPath))
                    {
                        // Create a 32x32 version for the icon
                        using (var resized = new Bitmap(bitmap, 32, 32))
                        {
                            return Icon.FromHandle(resized.GetHicon());
                        }
                    }
                }
                else
                {
                    return CreateFallbackIcon();
                }
            }
            catch
            {
                return CreateFallbackIcon();
            }
        }

        public static Icon CreateFallbackIcon()
        {
            var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Create a BitLocker-style shield icon
                using (var blueBrush = new SolidBrush(Color.FromArgb(0, 120, 215)))
                using (var whiteBrush = new SolidBrush(Color.White))
                using (var darkBrush = new SolidBrush(Color.FromArgb(0, 90, 180)))
                {
                    // Draw shield background
                    var shieldPoints = new Point[]
                    {
                        new Point(16, 3),
                        new Point(27, 9),
                        new Point(27, 19),
                        new Point(16, 29),
                        new Point(5, 19),
                        new Point(5, 9)
                    };
                    
                    g.FillPolygon(blueBrush, shieldPoints);
                    
                    // Draw lock symbol
                    // Lock body
                    g.FillRectangle(whiteBrush, 11, 17, 10, 8);
                    g.DrawRectangle(Pens.Black, 11, 17, 10, 8);
                    
                    // Lock shackle
                    using (var pen = new Pen(Color.Black, 2))
                    {
                        g.DrawArc(pen, 13, 11, 6, 8, 0, 180);
                    }
                    
                    // Lock keyhole
                    g.FillEllipse(Brushes.Black, 15, 20, 2, 2);
                }
            }
            
            return Icon.FromHandle(bitmap.GetHicon());
        }
    }
}