using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace XboxKit.GUI
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource? _cts;
        private bool _isProcessing = false;

        public MainWindow()
        {
            InitializeComponent();
            AppendLog("XboxKit GUI v0.7.0 준비 완료");
            AppendLog("원하는 디스크 이미지(.iso / .xiso)를 선택하거나 창 위로 드래그 앤 드롭하세요.\n");
        }

        #region 드래그 앤 드롭 및 파일 탐색

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                TxtInputPath.Text = files[0];

                // 여러 파일이 함께 드롭되었고 Rebuild 탭인 경우 보조 파일 목록에도 추가
                if (files.Length > 1 && TabMainMode.SelectedIndex == 1)
                {
                    for (int i = 1; i < files.Length; i++)
                    {
                        if (!ListRebuildFiles.Items.Contains(files[i]))
                        {
                            ListRebuildFiles.Items.Add(files[i]);
                        }
                    }
                }
            }
        }

        private void BtnBrowseFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "ISO 또는 XISO 파일 선택",
                Filter = "디스크 이미지 파일 (*.iso;*.xiso)|*.iso;*.xiso|모든 파일 (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                TxtInputPath.Text = dlg.FileName;
            }
        }

        private void BtnClearFile_Click(object sender, RoutedEventArgs e)
        {
            TxtInputPath.Text = string.Empty;
        }

        private void TxtInputPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            string path = TxtInputPath.Text.Trim();
            if (string.IsNullOrEmpty(path))
            {
                TxtFileDetection.Text = "ISO 또는 XISO 파일을 창 위에 드래그 앤 드롭하거나 [파일 찾기]를 클릭하세요.";
                return;
            }

            if (!File.Exists(path))
            {
                TxtFileDetection.Text = "지정한 파일이 존재하지 않습니다.";
                return;
            }

            FileInfo fi = new FileInfo(path);
            double sizeGb = (double)fi.Length / (1024 * 1024 * 1024);
            string ext = fi.Extension.ToLowerInvariant();

            if (ext == ".xiso")
            {
                TxtFileDetection.Text = $"XISO 파일 감지 ({sizeGb:F2} GB) - 변환 또는 Rebuild(복원) 작업 가능";
            }
            else if (ext == ".iso")
            {
                TxtFileDetection.Text = $"ISO 디스크 이미지 감지 ({sizeGb:F2} GB)";
            }
            else
            {
                TxtFileDetection.Text = $"파일: {fi.Name} ({sizeGb:F2} GB)";
            }
        }

        #endregion

        #region 프리셋 및 모드 선택

        private void RbPreset_Checked(object sender, RoutedEventArgs e)
        {
            if (GrpCustomOptions == null) return;

            if (RbPresetCustom.IsChecked == true)
            {
                GrpCustomOptions.IsEnabled = true;
            }
            else
            {
                GrpCustomOptions.IsEnabled = false;
                SyncPresetToCheckboxes();
            }
        }

        private void SyncPresetToCheckboxes()
        {
            // 초기화
            ChkExtractXiso.IsChecked = false;
            ChkTrim.IsChecked = false;
            ChkWipe.IsChecked = false;
            ChkExtractVideo.IsChecked = false;
            ChkExtractFiller.IsChecked = false;
            ChkExtractSeed.IsChecked = false;
            ChkExtractUpdate.IsChecked = false;
            ChkExtractOutput.IsChecked = false;
            ChkExtractSkeleton.IsChecked = false;
            ChkExtractZar.IsChecked = false;

            if (RbPresetBest.IsChecked == true)
            {
                // -b : -twx
                ChkExtractXiso.IsChecked = true;
                ChkTrim.IsChecked = true;
                ChkWipe.IsChecked = true;
            }
            else if (RbPresetAll.IsChecked == true)
            {
                // -a : -rstuvwx
                ChkExtractFiller.IsChecked = true;
                ChkExtractSeed.IsChecked = true;
                ChkTrim.IsChecked = true;
                ChkExtractUpdate.IsChecked = true;
                ChkExtractVideo.IsChecked = true;
                ChkWipe.IsChecked = true;
                ChkExtractXiso.IsChecked = true;
            }
            else if (RbPresetCompress.IsChecked == true)
            {
                // -c : -puvz
                ChkExtractSkeleton.IsChecked = true;
                ChkExtractUpdate.IsChecked = true;
                ChkExtractVideo.IsChecked = true;
                ChkExtractZar.IsChecked = true;
            }
        }

        private void TabMainMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BtnStart == null) return;
            if (TabMainMode.SelectedIndex == 1)
            {
                BtnStart.Content = "▶ 원본 복원 시작";
            }
            else
            {
                BtnStart.Content = "▶ 추출/변환 시작";
            }
        }

        #endregion

        #region Rebuild 추가 파일 관리

        private void BtnAddRebuildFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "보조 파일 선택 (.filler, .seed, .video.iso, su20076000_00000000 등)",
                Filter = "모든 관련 파일 (*.*)|*.*",
                Multiselect = true
            };

            if (dlg.ShowDialog() == true)
            {
                foreach (string f in dlg.FileNames)
                {
                    if (!ListRebuildFiles.Items.Contains(f))
                    {
                        ListRebuildFiles.Items.Add(f);
                    }
                }
            }
        }

        private void BtnRemoveRebuildFile_Click(object sender, RoutedEventArgs e)
        {
            var selected = new List<object>();
            foreach (var item in ListRebuildFiles.SelectedItems)
            {
                selected.Add(item);
            }
            foreach (var item in selected)
            {
                ListRebuildFiles.Items.Remove(item);
            }
        }

        private void BtnClearRebuildFiles_Click(object sender, RoutedEventArgs e)
        {
            ListRebuildFiles.Items.Clear();
        }

        #endregion

        #region 실행 및 로그 제어

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            string inputPath = TxtInputPath.Text.Trim();
            if (string.IsNullOrEmpty(inputPath))
            {
                MessageBox.Show("입력 파일 경로를 지정해 주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!File.Exists(inputPath))
            {
                MessageBox.Show($"파일을 찾을 수 없습니다:\n{inputPath}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            List<string> args = new List<string>();

            // 덮어쓰기 기본 허용(-y) 적용하여 자동 진행
            args.Add("-y");

            if (TabMainMode.SelectedIndex == 1) // Rebuild Mode
            {
                // Rebuild 모드: 옵션 없이 <input.xiso> [추가 파일들...]
                args.Clear(); // Rebuild 모드는 옵션 플래그를 사용하지 않음
                args.Add(inputPath);
                foreach (var item in ListRebuildFiles.Items)
                {
                    if (item is string extraPath && File.Exists(extraPath) && !extraPath.Equals(inputPath, StringComparison.OrdinalIgnoreCase))
                    {
                        args.Add(extraPath);
                    }
                }
            }
            else // Extract & Convert Mode
            {
                if (RbPresetBest.IsChecked == true)
                {
                    args.Add("-b");
                }
                else if (RbPresetAll.IsChecked == true)
                {
                    args.Add("-a");
                }
                else if (RbPresetCompress.IsChecked == true)
                {
                    args.Add("-c");
                }
                else // Custom
                {
                    if (ChkExtractXiso.IsChecked == true) args.Add("-x");
                    if (ChkTrim.IsChecked == true) args.Add("-t");
                    if (ChkWipe.IsChecked == true) args.Add("-w");
                    if (ChkExtractVideo.IsChecked == true) args.Add("-v");
                    if (ChkExtractFiller.IsChecked == true) args.Add("-r");
                    if (ChkExtractSeed.IsChecked == true) args.Add("-s");
                    if (ChkExtractUpdate.IsChecked == true) args.Add("-u");
                    if (ChkExtractOutput.IsChecked == true) args.Add("-o");
                    if (ChkExtractSkeleton.IsChecked == true) args.Add("-p");
                    if (ChkExtractZar.IsChecked == true) args.Add("-z");

                    if (args.Count <= 1) // -y 만 있는 경우
                    {
                        MessageBox.Show("최소 하나 이상의 추출/변환 옵션을 선택해야 합니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                args.Add(inputPath);
            }

            SetProcessingState(true);
            _cts = new CancellationTokenSource();

            AppendLog($"\n========================================================");
            AppendLog($"[작업 시작] {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            AppendLog($"명령행 인자: {string.Join(" ", args)}");
            AppendLog($"========================================================");

            TextWriter originalOut = Console.Out;
            TextWriter originalErr = Console.Error;

            ConsoleRedirectWriter customWriter = new ConsoleRedirectWriter(line =>
            {
                Dispatcher.Invoke(() => AppendLog(line));
            });

            bool success = false;
            try
            {
                Console.SetOut(customWriter);
                Console.SetError(customWriter);

                await Task.Run(() =>
                {
                    Options? opts = Helpers.ParseArgs(args.ToArray());
                    if (opts == null)
                    {
                        Console.WriteLine("[ERROR] 인자 구문 분석 실패");
                        return;
                    }

                    switch (opts.Mode)
                    {
                        case Mode.ExtractRedump:
                            ExtractRedump.Run(opts);
                            break;
                        case Mode.ExtractVideo:
                            ExtractVideo.Run(opts);
                            break;
                        case Mode.ProcessXISO:
                            ProcessXISO.Run(opts);
                            break;
                        case Mode.RebuildISO:
                            RebuildISO.Run(opts);
                            break;
                    }
                    success = true;
                }, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                AppendLog("\n[알림] 사용자에 의해 작업이 취소되었습니다.");
            }
            catch (Exception ex)
            {
                AppendLog($"\n[ERROR] 처리 중 예외 발생: {ex.Message}");
                AppendLog(ex.StackTrace ?? string.Empty);
            }
            finally
            {
                customWriter.Flush();
                Console.SetOut(originalOut);
                Console.SetError(originalErr);

                SetProcessingState(false);
                AppendLog($"[작업 종료] {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

                if (success)
                {
                    MessageBox.Show("작업이 성공적으로 완료되었습니다!", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing && _cts != null)
            {
                _cts.Cancel();
                BtnCancel.IsEnabled = false;
                AppendLog("[취소 요청됨] 현재 작업 중단 중...");
            }
        }

        private void SetProcessingState(bool isProcessing)
        {
            _isProcessing = isProcessing;
            BtnStart.IsEnabled = !isProcessing;
            BtnCancel.IsEnabled = isProcessing;
            BtnBrowseFile.IsEnabled = !isProcessing;
            BtnClearFile.IsEnabled = !isProcessing;
            TabMainMode.IsEnabled = !isProcessing;

            if (isProcessing)
            {
                TxtStatus.Text = "상태: 작업 진행 중...";
                PbProgress.IsIndeterminate = true;
            }
            else
            {
                TxtStatus.Text = "상태: 대기 중";
                PbProgress.IsIndeterminate = false;
                PbProgress.Value = 0;
            }
        }

        private void AppendLog(string message)
        {
            TxtLog.AppendText(message + "\n");
            TxtLog.ScrollToEnd();
        }

        private void BtnCopyLog_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TxtLog.Text))
            {
                Clipboard.SetText(TxtLog.Text);
                MessageBox.Show("로그가 클립보드에 복사되었습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            TxtLog.Clear();
        }

        #endregion
    }
}
