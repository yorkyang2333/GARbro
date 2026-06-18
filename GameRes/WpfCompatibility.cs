using System;
using System.Collections.Generic;
using System.IO;

namespace System.Windows.Media
{
    public struct Color
    {
        public byte A, R, G, B;
        public static Color FromArgb(byte a, byte r, byte g, byte b) { return new Color { A = a, R = r, G = g, B = b }; }
        public static Color FromRgb(byte r, byte g, byte b) { return new Color { A = 255, R = r, G = g, B = b }; }
        public static Color Subtract(Color c1, Color c2) { return new Color { A = (byte)(c1.A - c2.A), R = (byte)(c1.R - c2.R), G = (byte)(c1.G - c2.G), B = (byte)(c1.B - c2.B) }; }
        public static Color operator -(Color c1, Color c2) => Subtract(c1, c2);
        public static Color operator +(Color c1, Color c2) => new Color();
        public static bool operator ==(Color c1, Color c2) => c1.A == c2.A && c1.R == c2.R && c1.G == c2.G && c1.B == c2.B;
        public static bool operator !=(Color c1, Color c2) => !(c1 == c2);
        public override bool Equals(object obj) => obj is Color c && this == c;
        public override int GetHashCode() => A ^ R ^ G ^ B;
    }

    public class PixelFormat
    {
        public int BitsPerPixel { get; set; }
        public override bool Equals(object obj) => obj is PixelFormat p && p.BitsPerPixel == BitsPerPixel;
        public override int GetHashCode() => BitsPerPixel.GetHashCode();
        public static bool operator ==(PixelFormat left, PixelFormat right) => object.Equals(left, right);
        public static bool operator !=(PixelFormat left, PixelFormat right) => !object.Equals(left, right);
    }

    public static class PixelFormats
    {
        public static readonly PixelFormat Bgra32 = new PixelFormat { BitsPerPixel = 32 };
        public static readonly PixelFormat Bgr32 = new PixelFormat { BitsPerPixel = 32 };
        public static readonly PixelFormat Bgr24 = new PixelFormat { BitsPerPixel = 24 };
        public static readonly PixelFormat Bgr565 = new PixelFormat { BitsPerPixel = 16 };
        public static readonly PixelFormat Bgr555 = new PixelFormat { BitsPerPixel = 16 };
        public static readonly PixelFormat Pbgra32 = new PixelFormat { BitsPerPixel = 32 };
        public static readonly PixelFormat BlackWhite = new PixelFormat { BitsPerPixel = 1 };
        public static readonly PixelFormat Gray8 = new PixelFormat { BitsPerPixel = 8 };
        public static readonly PixelFormat Gray16 = new PixelFormat { BitsPerPixel = 16 };
        public static readonly PixelFormat Gray4 = new PixelFormat { BitsPerPixel = 4 };
        public static readonly PixelFormat Indexed8 = new PixelFormat { BitsPerPixel = 8 };
        public static readonly PixelFormat Indexed4 = new PixelFormat { BitsPerPixel = 4 };
        public static readonly PixelFormat Indexed2 = new PixelFormat { BitsPerPixel = 2 };
        public static readonly PixelFormat Indexed1 = new PixelFormat { BitsPerPixel = 1 };
        public static readonly PixelFormat Rgb24 = new PixelFormat { BitsPerPixel = 24 };
    }

    public class ScaleTransform
    {
        public double ScaleX { get; set; } = 1;
        public double ScaleY { get; set; } = 1;
    }
    
    public class DrawingContext : IDisposable {
        public void DrawImage(System.Windows.Media.Imaging.BitmapSource source, System.Windows.Rect rectangle) { }
        public void Close() { }
        public void Dispose() { }
    }
    public class DrawingVisual {
        public DrawingContext RenderOpen() => new DrawingContext();
    }
}

namespace System.Windows.Media.Imaging
{
    public enum BitmapCreateOptions { None = 0, PreservePixelFormat = 1, IgnoreImageCache = 8, IgnoreColorProfile = 4 }
    public enum BitmapCacheOption { Default = 0, OnDemand = 1, OnLoad = 2, None = 3 }
    public enum TiffCompressOption { Default = 0, None = 1, Ccitt3 = 2, Ccitt4 = 3, Lzw = 4, Rle = 5, Zip = 6 }

    public class BitmapPalette
    {
        public IList<Color> Colors { get; }
        public BitmapPalette(IList<Color> colors) { Colors = colors; }
    }

    public class BitmapSource
    {
        public int PixelWidth { get; set; }
        public int PixelHeight { get; set; }
        public PixelFormat Format { get; set; }
        
        public double DpiX { get; set; } = 96.0;
        public double DpiY { get; set; } = 96.0;
        
        public Array Pixels { get; set; }
        public int Stride { get; set; }
        public BitmapPalette Palette { get; set; }

        public static BitmapSource Create(int pixelWidth, int pixelHeight, double dpiX, double dpiY, PixelFormat pixelFormat, BitmapPalette palette, Array pixels, int stride)
        {
            return new BitmapSource {
                PixelWidth = pixelWidth,
                PixelHeight = pixelHeight,
                Format = pixelFormat,
                Pixels = pixels,
                Stride = stride,
                Palette = palette
            };
        }

        public void Freeze() { }
        
        public void CopyPixels(Array pixels, int stride, int offset)
        {
            if (Pixels != null)
                Buffer.BlockCopy(Pixels, 0, pixels, offset, Buffer.ByteLength(Pixels));
        }
        
        public void CopyPixels(System.Windows.Int32Rect sourceRect, Array pixels, int stride, int offset)
        {
            if (Pixels != null)
                Buffer.BlockCopy(Pixels, 0, pixels, offset, Math.Min(Buffer.ByteLength(Pixels), Buffer.ByteLength(pixels)));
        }

        public void CopyPixels(System.Windows.Int32Rect sourceRect, IntPtr buffer, int bufferSize, int stride) {}
    }

    public class TransformedBitmap : BitmapSource
    {
        public TransformedBitmap(BitmapSource source, ScaleTransform transform)
        {
            PixelWidth = source.PixelWidth;
            PixelHeight = source.PixelHeight;
            Format = source.Format;
            Pixels = source.Pixels; 
            Stride = source.Stride;
            Palette = source.Palette;
        }
    }
    
    public class FormatConvertedBitmap : BitmapSource {
        public FormatConvertedBitmap() { }
        public FormatConvertedBitmap(BitmapSource s, PixelFormat f, BitmapPalette p, double alpha) {
            Source = s; DestinationFormat = f; EndInit();
        }
        public BitmapSource Source { get; set; }
        public PixelFormat DestinationFormat { get; set; }
        public void BeginInit() {}
        public void EndInit() {
            if (Source != null) { PixelWidth = Source.PixelWidth; PixelHeight = Source.PixelHeight; Pixels = Source.Pixels; Stride = Source.Stride; Palette = Source.Palette; }
            if (DestinationFormat != null) Format = DestinationFormat;
        }
    }
    
    public abstract class BitmapEncoder
    {
        public IList<BitmapFrame> Frames { get; } = new List<BitmapFrame>();
        public abstract void Save(Stream stream);
    }
    
    public class BitmapFrame : BitmapSource
    {
        public static BitmapFrame Create(BitmapSource source) => new BitmapFrame { Pixels = source?.Pixels, PixelWidth = source?.PixelWidth ?? 0, PixelHeight = source?.PixelHeight ?? 0, Format = source?.Format, Palette = source?.Palette, Stride = source?.Stride ?? 0 };
        public static BitmapFrame Create(BitmapSource source, object thumbnail, object metadata, object colorContexts) => Create(source);
    }
    
    public class PngBitmapEncoder : BitmapEncoder { 
        public bool Interlace { get; set; }
        public override void Save(Stream stream) {} 
    }
    public class JpegBitmapEncoder : BitmapEncoder { 
        public int QualityLevel { get; set; }
        public override void Save(Stream stream) {} 
    }
    public class BmpBitmapEncoder : BitmapEncoder { public override void Save(Stream stream) {} }
    public class TiffBitmapEncoder : BitmapEncoder { 
        public TiffCompressOption Compression { get; set; }
        public override void Save(Stream stream) {} 
    }
    
    public class BitmapDecoder {
        public IList<BitmapFrame> Frames { get; } = new List<BitmapFrame> { new BitmapFrame() };
        public static BitmapDecoder Create(Stream stream, BitmapCreateOptions createOptions, BitmapCacheOption cacheOption) => new BitmapDecoder();
    }
    public class JpegBitmapDecoder : BitmapDecoder { public JpegBitmapDecoder(Stream s, BitmapCreateOptions c, BitmapCacheOption o) {} }
    public class PngBitmapDecoder : BitmapDecoder { public PngBitmapDecoder(Stream s, BitmapCreateOptions c, BitmapCacheOption o) {} }
    public class BmpBitmapDecoder : BitmapDecoder { public BmpBitmapDecoder(Stream s, BitmapCreateOptions c, BitmapCacheOption o) {} }
    public class TiffBitmapDecoder : BitmapDecoder { public TiffBitmapDecoder(Stream s, BitmapCreateOptions c, BitmapCacheOption o) {} }

    public class WriteableBitmap : BitmapSource
    {
        public int BackBufferStride { get; set; }
        public IntPtr BackBuffer { get; set; }
        public WriteableBitmap(int pixelWidth, int pixelHeight, double dpiX, double dpiY, PixelFormat pixelFormat, BitmapPalette palette) {
            PixelWidth = pixelWidth; PixelHeight = pixelHeight; Format = pixelFormat; Palette = palette;
            Stride = (pixelWidth * pixelFormat.BitsPerPixel + 7) / 8;
            Pixels = new byte[Stride * pixelHeight];
        }
        public WriteableBitmap(BitmapSource source) : this(source.PixelWidth, source.PixelHeight, source.DpiX, source.DpiY, source.Format, source.Palette) {}
        public void WritePixels(System.Windows.Int32Rect sourceRect, Array pixels, int stride, int offset) {
            if (Pixels != null && pixels != null) Buffer.BlockCopy(pixels, 0, Pixels, offset, Math.Min(Buffer.ByteLength(pixels), Buffer.ByteLength(Pixels)));
        }
        public void WritePixels(System.Windows.Int32Rect sourceRect, IntPtr buffer, int bufferSize, int stride) { }
        public void WritePixels(System.Windows.Int32Rect sourceRect, Array pixels, int stride, int x, int y) { }
        public void WritePixels(System.Windows.Int32Rect sourceRect, IntPtr buffer, int bufferSize, int stride, int x, int y) { }
        public void Lock() { }
        public void Unlock() { }
        public void AddDirtyRect(System.Windows.Int32Rect dirtyRect) { }
    }
    
    public class RenderTargetBitmap : BitmapSource {
        public RenderTargetBitmap(int pixelWidth, int pixelHeight, double dpiX, double dpiY, PixelFormat pixelFormat) { }
        public void Render(DrawingVisual visual) { }
    }
    
    public class CroppedBitmap : BitmapSource {
        public CroppedBitmap(BitmapSource source, System.Windows.Int32Rect sourceRect) { }
    }
}

namespace System.Windows
{
    public struct Int32Rect
    {
        public int X, Y, Width, Height;
        public Int32Rect(int x, int y, int width, int height) { X = x; Y = y; Width = width; Height = height; }
        public static Int32Rect Empty = new Int32Rect();
        public bool HasArea => Width > 0 && Height > 0;
    }
    
    public struct Rect {
        public double X, Y, Width, Height;
        public Rect(double x, double y, double width, double height) { X=x; Y=y; Width=width; Height=height; }
    }
}
