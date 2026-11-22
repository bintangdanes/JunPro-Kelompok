using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OpenCvSharp;

namespace Trashury;

public partial class KameraWindow : System.Windows.Window
{
    private readonly VideoCapture _capture;
    private readonly DispatcherTimer _timer;
    private Mat? _frameTerkini;

    public byte[]? HasilGambar { get; private set; }

    public KameraWindow()
    {
        InitializeComponent();

        _capture = new VideoCapture(0);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _timer.Tick += Timer_Tick;

        Loaded += (_, _) =>
        {
            if (!_capture.IsOpened())
            {
                MessageBox.Show("Kamera/webcam tidak ditemukan atau sedang dipakai aplikasi lain.", "Kamera Tidak Tersedia", MessageBoxButton.OK, MessageBoxImage.Warning);
                DialogResult = false;
                Close();
                return;
            }
            _timer.Start();
        };
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        using var frame = new Mat();
        if (!_capture.Read(frame) || frame.Empty())
            return;

        _frameTerkini?.Dispose();
        _frameTerkini = frame.Clone();

        var bitmap = BitmapSource.Create(
            frame.Width,
            frame.Height,
            96,
            96,
            PixelFormats.Bgr24,
            null,
            frame.Data,
            (int)(frame.Step() * frame.Height),
            (int)frame.Step());
        bitmap.Freeze();

        PreviewKamera.Source = bitmap;
    }

    private void Ambil_Click(object sender, RoutedEventArgs e)
    {
        if (_frameTerkini == null || _frameTerkini.Empty() || !Cv2.ImEncode(".jpg", _frameTerkini, out byte[] bytes))
        {
            MessageBox.Show("Frame kamera belum siap.", "Kamera", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        HasilGambar = bytes;
        DialogResult = true;
        Close();
    }

    private void Batal_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        _frameTerkini?.Dispose();
        _capture.Release();
        _capture.Dispose();
        base.OnClosed(e);
    }
}
