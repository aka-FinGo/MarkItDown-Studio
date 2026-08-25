using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MarkItDown.App;

public partial class SnippingWindow : Window
{
    private Point _startPoint;
    private bool _isSelecting;
    public byte[]? CapturedImageBytes { get; private set; }

    public SnippingWindow()
    {
        InitializeComponent();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _startPoint = e.GetPosition(this);
            _isSelecting = true;
            SelectionBox.Visibility = Visibility.Visible;
            Canvas.SetLeft(SelectionBox, _startPoint.X);
            Canvas.SetTop(SelectionBox, _startPoint.Y);
            SelectionBox.Width = 0;
            SelectionBox.Height = 0;
        }
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isSelecting)
        {
            var current = e.GetPosition(this);
            var x = Math.Min(_startPoint.X, current.X);
            var y = Math.Min(_startPoint.Y, current.Y);
            var w = Math.Abs(current.X - _startPoint.X);
            var h = Math.Abs(current.Y - _startPoint.Y);

            Canvas.SetLeft(SelectionBox, x);
            Canvas.SetTop(SelectionBox, y);
            SelectionBox.Width = w;
            SelectionBox.Height = h;
        }
    }

    private void Window_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isSelecting)
        {
            _isSelecting = false;
            var current = e.GetPosition(this);
            var x = (int)Math.Min(_startPoint.X, current.X);
            var y = (int)Math.Min(_startPoint.Y, current.Y);
            var w = (int)Math.Abs(current.X - _startPoint.X);
            var h = (int)Math.Abs(current.Y - _startPoint.Y);

            // Hide overlay before capturing to avoid capturing the dark screen
            Visibility = Visibility.Hidden;

            if (w > 10 && h > 10)
            {
                // Capture the exact physical screen pixels with DPI scale factor
                var source = PresentationSource.FromVisual(this);
                var dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
                var dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

                var screenX = (int)(x * dpiX);
                var screenY = (int)(y * dpiY);
                var screenW = (int)(w * dpiX);
                var screenH = (int)(h * dpiY);

                CapturedImageBytes = CaptureScreenRectangle(screenX, screenY, screenW, screenH);
            }

            DialogResult = CapturedImageBytes != null && CapturedImageBytes.Length > 0;
            Close();
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }

    // Win32 GDI+ Screen Capture
    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

    private const int SRCCOPY = 0x00CC0020;

    private static byte[]? CaptureScreenRectangle(int x, int y, int width, int height)
    {
        try
        {
            var hScreenDC = GetDC(IntPtr.Zero);
            var hMemoryDC = CreateCompatibleDC(hScreenDC);
            var hBitmap = CreateCompatibleBitmap(hScreenDC, width, height);
            var hOldBitmap = SelectObject(hMemoryDC, hBitmap);

            BitBlt(hMemoryDC, 0, 0, width, height, hScreenDC, x, y, SRCCOPY);

            var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            SelectObject(hMemoryDC, hOldBitmap);
            DeleteObject(hBitmap);
            DeleteDC(hMemoryDC);
            ReleaseDC(IntPtr.Zero, hScreenDC);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            using var ms = new MemoryStream();
            encoder.Save(ms);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Snipping] Screen capture xatosi: {ex.Message}");
            return null;
        }
    }
}
