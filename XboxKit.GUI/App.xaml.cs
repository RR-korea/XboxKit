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

            // 1. 글로벌 미처리 예외 핸들러 등록 (프로그램 급작스러운 종료 방지)
            this.DispatcherUnhandledException += (s, args) =>
            {
                string msg = $"[UI 스레드 오류] {args.Exception.Message}\n\n스택 트레이스:\n{args.Exception.StackTrace}";
                FileLogger.Instance.Log(msg);
                MessageBox.Show(msg, "치명적 오류 발생", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true; // 강제 종료 방지
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    string msg = $"[도메인 미처리 오류] {ex.Message}\n\n스택 트레이스:\n{ex.StackTrace}";
                    FileLogger.Instance.Log(msg);
                    MessageBox.Show(msg, "치명적 오류 발생", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                string msg = $"[비동기 태스크 오류] {args.Exception.Message}";
                FileLogger.Instance.Log(msg);
                args.SetObserved(); // 강제 종료 방지
            };

            // 2. 스크린샷 모드 처리
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
                    FileLogger.Instance.Log($"스크린샷 실패: {ex}");
                }
                Shutdown();
                return;
            }

            // 3. 정상 GUI 모드 시작
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;
            var mainWindow = new MainWindow();
            this.MainWindow = mainWindow;
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            FileLogger.Instance.Close();
            base.OnExit(e);
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
