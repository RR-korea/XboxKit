using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace XboxKit.GUI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            int screenshotIdx = Array.IndexOf(e.Args, "--screenshot");
            if (screenshotIdx >= 0 && screenshotIdx < e.Args.Length - 1)
            {
                string outputPath = e.Args[screenshotIdx + 1];
                try
                {
                    CaptureWindowScreenshot(outputPath);
                }
                catch (Exception ex)
                {
                    File.WriteAllText("screenshot_error.txt", ex.ToString());
                }
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private void CaptureWindowScreenshot(string outputPath)
        {
            var window = new MainWindow();
            window.Width = 920;
            window.Height = 780;
            
            // WPF 창 렌더링을 위해 Show 후 Hide 하거나 가상 레이아웃 설정
            string? dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            window.Show();
            window.Measure(new Size(920, 780));
            window.Arrange(new Rect(0, 0, 920, 780));
            window.UpdateLayout();

            int width = (int)window.ActualWidth;
            int height = (int)window.ActualHeight;
            if (width <= 0) width = 920;
            if (height <= 0) height = 780;

            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(window);
            window.Close();

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            using var fs = File.Open(outputPath, FileMode.Create, FileAccess.Write);
            encoder.Save(fs);
        }
    }
}
