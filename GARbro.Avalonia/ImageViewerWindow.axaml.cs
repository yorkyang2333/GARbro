using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GameRes;

namespace GARbro.Avalonia;

public partial class ImageViewerWindow : Window
{
    public ImageViewerWindow()
    {
        InitializeComponent();
    }

    public void LoadImage(Entry entry, ImageData imageData)
    {
        Title = $"{entry.Name} - Image Viewer";

        // GameRes typically outputs BGRA32 / Bgra8888 formatted pixels
        int width = (int)imageData.Width;
        int height = (int)imageData.Height;
        
        // Avalonia's WriteableBitmap is very efficient for direct pixel manipulation
        var writeableBitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);

        var pixels = imageData.Bitmap.Pixels as byte[];
        if (pixels != null)
        {
            using (var fb = writeableBitmap.Lock())
            {
                Marshal.Copy(pixels, 0, fb.Address, pixels.Length);
            }
        }

        MainImage.Source = writeableBitmap;
    }
}
