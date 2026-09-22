using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
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

            string logPath = FileLogger.Instance.LogFilePath;
            TxtLogFilePath.Text = string.IsNullOrEmpty(logPath) 
                ? "로그 파일: 비활성화됨" 
                : $"로그 파일: {Path.GetFileName(logPath)}";

            AppendLog("XboxKit GUI v0.7.0 준비 완료");
            AppendLog($"세션 로그 파일: {logPath}");
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

            try
            {
                FileInfo fi = new FileInfo(path);
                double sizeGb = (double)fi.Length / (1024 * 1024 * 1024);
                string ext = fi.Extension.ToLowerInvariant();

                int redumpType = Array.IndexOf(LibXGD.XGD.REDUMP_ISO_LENGTH, fi.Length);
                int videoType = Array.IndexOf(LibXGD.XGD.VIDEO_LENGTH, fi.Length);
                int xisoType = Array.IndexOf(LibXGD.XGD.XISO_LENGTH, fi.Length);

                if (redumpType >= 0)
                {
                    int xgdType = LibXGD.XGD.GetXGDType(redumpType);
                    string xgdName = xgdType == 0 ? "XGD1 (Xbox)" : (xgdType == 3 ? "XGD3 (Xbox 360)" : "XGD2 (Xbox 360)");
                    TxtFileDetection.Text = $"Redump ISO 감지: {xgdName} [{fi.Length:N0} 바이트 / {sizeGb:F2} GB] - [추출 및 변환] 탭 추천";
                }
                else if (xisoType >= 0 || ext == ".xiso")
                {
                    TxtFileDetection.Text = $"XISO (게임 파티션) 감지 [{fi.Length:N0} 바이트 / {sizeGb:F2} GB] - [원본 복원] 또는 변환 가능";
                }
                else if (videoType >= 0)
                {
                    TxtFileDetection.Text = $"Video ISO (비디오 파티션) 감지 [{fi.Length:N0} 바이트 / {sizeGb:F2} GB]";
                }
                else
                {
                    TxtFileDetection.Text = $"파일: {fi.Name} ({sizeGb:F2} GB / {fi.Length:N0} 바이트)";
                }
            }
            catch (Exception ex)
            {
                TxtFileDetection.Text = $"파일 분석 오류: {ex.Message}";
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
                ChkExtractXiso.IsChecked = true;
                ChkTrim.IsChecked = true;
                ChkWipe.IsChecked = true;
            }
            else if (RbPresetAll.IsChecked == true)
            {
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

        #region 사전 분석 및 테스트 (Dry-Run / Inspect)

        private async void BtnTestInspect_Click(object sender, RoutedEventArgs e)
        {
            string inputPath = TxtInputPath.Text.Trim();
            if (string.IsNullOrEmpty(inputPath))
            {
                MessageBox.Show("분석할 파일 경로를 지정해 주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!File.Exists(inputPath))
            {
                MessageBox.Show($"파일을 찾을 수 없습니다:\n{inputPath}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SetProcessingState(true);
            AppendLog("\n========================================================");
            AppendLog($"[사전 분석 및 유효성 검사 시작] {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            AppendLog($"대상 파일: {inputPath}");
            AppendLog("========================================================");

            await Task.Run(() =>
            {
                try
                {
                    FileInfo fi = new FileInfo(inputPath);
                    long fileSize = fi.Length;
                    double sizeGb = (double)fileSize / (1024 * 1024 * 1024);

                    AppendLog($"\n[1] 파일 기본 정보");
                    AppendLog($" - 파일명: {fi.Name}");
                    AppendLog($" - 폴더: {fi.DirectoryName}");
                    AppendLog($" - 파일 크기: {fileSize:N0} 바이트 ({sizeGb:F2} GB)");

                    int redumpType = Array.IndexOf(LibXGD.XGD.REDUMP_ISO_LENGTH, fileSize);
                    int videoType = Array.IndexOf(LibXGD.XGD.VIDEO_LENGTH, fileSize);
                    int xisoType = Array.IndexOf(LibXGD.XGD.XISO_LENGTH, fileSize);

                    AppendLog($"\n[2] 디스크 이미지 규격 식별");
                    if (redumpType >= 0)
                    {
                        int xgdType = LibXGD.XGD.GetXGDType(redumpType);
                        string xgdName = xgdType == 0 ? "XGD1 (Xbox 오리지널)" : (xgdType == 3 ? "XGD3 (Xbox 360 후기형)" : "XGD2 (Xbox 360)");
                        AppendLog($" [OK] 표준 Redump ISO로 식별되었습니다! (타입 인덱스: {redumpType}, 규격: {xgdName})");
                        AppendLog($"  -> 추천 작업: [추출 및 변환] 탭에서 에뮬레이터용 XISO 변환(-b) 또는 풀 백업(-a)");
                        if (TabMainMode.SelectedIndex == 1)
                        {
                            AppendLog($" [주의] 현재 '원본 복원(Rebuild)' 탭이 선택되어 있습니다.");
                            AppendLog($"  -> 이 파일은 이미 온전한 Redump ISO이므로 복원이 필요하지 않습니다.");
                        }
                    }
                    else if (xisoType >= 0 || fi.Extension.Equals(".xiso", StringComparison.OrdinalIgnoreCase))
                    {
                        AppendLog($" [OK] XISO (XDVDFS 게임 파티션)로 식별되었습니다! (타입 인덱스: {xisoType})");
                        AppendLog($"  -> [추출 및 변환] 탭: 게임 파일 폴더 추출(-o) 또는 ZArchive 압축(-c) 가능");
                        AppendLog($"  -> [원본 복원] 탭: 보조 파일(.video.iso, .filler 등)과 결합하여 Redump ISO 복원 가능");
                    }
                    else if (videoType >= 0)
                    {
                        AppendLog($" [OK] Video ISO (비디오 파티션)로 식별되었습니다! (타입 인덱스: {videoType})");
                    }
                    else
                    {
                        AppendLog($" [안내] 표준 고정 크기와 일치하지 않습니다. (트림된 XISO 또는 일반 ISO 파일일 수 있습니다.)");
                    }

                    // 헤더 유효성 검사
                    AppendLog($"\n[3] XDVDFS 파일시스템 무결성 검사");
                    using (FileStream fs = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        bool isXiso = LibXGD.XDVDFS.IsValidXISO(fs);
                        if (isXiso)
                        {
                            AppendLog(" [OK] 유효한 XDVDFS (XISO) 시그니처가 확인되었습니다.");
                        }
                        else
                        {
                            AppendLog(" [안내] 시작 섹터에서 XDVDFS 매직을 찾지 못했습니다. (Redump ISO의 경우 내부 오프셋에 위치합니다)");
                        }
                    }

                    // Rebuild 관련 보조 파일 자동 탐색 검사
                    string dir = fi.DirectoryName ?? "";
                    string baseName = Path.GetFileNameWithoutExtension(inputPath);
                    AppendLog($"\n[4] Rebuild(복원) 연관 보조 파일 동반 여부 검사 (폴더 내 자동 스캔)");

                    string testVideo = Path.Combine(dir, $"{baseName}.video.iso");
                    string testFiller = Path.Combine(dir, $"{baseName}.filler");
                    string testSeed = Path.Combine(dir, $"{baseName}.seed");
                    string testUpdate = Path.Combine(dir, "su20076000_00000000");

                    AppendLog($" - Video ISO ({baseName}.video.iso): " + (File.Exists(testVideo) ? $"[발견됨] ({new FileInfo(testVideo).Length:N0} 바이트)" : "[없음]"));
                    AppendLog($" - Filler Data ({baseName}.filler): " + (File.Exists(testFiller) ? $"[발견됨] ({new FileInfo(testFiller).Length:N0} 바이트)" : "[없음]"));
                    AppendLog($" - Seed Data ({baseName}.seed): " + (File.Exists(testSeed) ? $"[발견됨] ({new FileInfo(testSeed).Length:N0} 바이트)" : "[없음]"));
                    AppendLog($" - System Update (su20076000_00000000): " + (File.Exists(testUpdate) ? $"[발견됨] ({new FileInfo(testUpdate).Length:N0} 바이트)" : "[없음]"));

                    AppendLog("\n========================================================");
                    AppendLog("사전 분석 완료! 세부 사항은 위 로그를 참고하세요.");
                    AppendLog("========================================================\n");
                }
                catch (Exception ex)
                {
                    AppendLog($"[분석 실패] {ex.Message}\n{ex.StackTrace}");
                }
            });

            SetProcessingState(false);
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

            FileInfo fi = new FileInfo(inputPath);
            int redumpType = Array.IndexOf(LibXGD.XGD.REDUMP_ISO_LENGTH, fi.Length);

            // Rebuild 모드인데 Redump ISO를 넣은 경우 친절한 안내 및 방어
            if (TabMainMode.SelectedIndex == 1 && redumpType >= 0)
            {
                var result = MessageBox.Show(
                    $"선택하신 파일은 이미 온전한 Redump ISO 디스크 이미지입니다. ({fi.Length:N0} 바이트)\n\n" +
                    "[원본 복원(Rebuild)]은 분할된 .xiso와 보조 파일들을 결합하여 Redump ISO로 되돌리는 기능입니다.\n" +
                    "Redump ISO에서 XISO 추출이나 최적화를 원하시면 [추출 및 변환] 탭을 사용하셔야 합니다.\n\n" +
                    "[추출 및 변환] 탭으로 전환하여 진행하시겠습니까?",
                    "작업 모드 확인",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    TabMainMode.SelectedIndex = 0;
                    return;
                }
                else if (result == MessageBoxResult.Cancel || result == MessageBoxResult.No)
                {
                    AppendLog("[작업 중단] Redump ISO에 대해 원본 복원이 취소되었습니다.");
                    return;
                }
            }

            List<string> args = new List<string>();

            // 안전한 덮어쓰기 기본 허용 플래그 (-y)
            args.Add("-y");

            if (TabMainMode.SelectedIndex == 1) // Rebuild Mode
            {
                // Rebuild 모드: <input.xiso> [추가 파일들...]
                // XboxKit CLI 규격상 옵션 플래그가 없어야 Rebuild 모드로 진입하므로
                // GUI에서는 -y 대신 Helpers.ParseArgs가 Rebuild 모드로 갈 수 있도록 설정
                args.Clear();
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

                    if (args.Count <= 1)
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
            AppendLog($"인자 목록: {string.Join(" ", args)}");
            AppendLog($"========================================================");

            TextWriter originalOut = Console.Out;
            TextWriter originalErr = Console.Error;
            TextReader originalIn = Console.In;

            // 콘솔 출력을 UI 및 로그 파일로 리디렉트
            ConsoleRedirectWriter customWriter = new ConsoleRedirectWriter(line =>
            {
                Dispatcher.BeginInvoke(new Action(() => AppendLog(line)));
            });

            bool success = false;
            try
            {
                Console.SetOut(customWriter);
                Console.SetError(customWriter);
                // Console.ReadLine() 호출 시 무한 블로킹을 방지하기 위해 자동 "Y" 입력 스트림 연결
                Console.SetIn(new StringReader("Y\nY\nY\n"));

                await Task.Run(() =>
                {
                    Options? opts = Helpers.ParseArgs(args.ToArray());
                    if (opts == null)
                    {
                        Console.WriteLine("[ERROR] 인자 구문 분석 실패");
                        return;
                    }

                    // GUI에서는 콘솔 대기 방지를 위해 AssumeYes를 강제 적용
                    opts.AssumeYes = true;
                    opts.AssumeNo = false;

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
                string errMsg = $"\n[ERROR] 처리 중 예외 발생: {ex.Message}\n{ex.StackTrace}";
                AppendLog(errMsg);
                FileLogger.Instance.Log(errMsg);
                MessageBox.Show($"작업 중 오류가 발생했습니다:\n{ex.Message}\n\n자세한 내용은 로그 창 또는 로그 파일을 확인하세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                customWriter.Flush();
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                Console.SetIn(originalIn);

                SetProcessingState(false);
                AppendLog($"[작업 종료] {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

                if (success)
                {
                    MessageBox.Show("작업이 완료되었습니다. 결과 로그를 확인하세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
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
            BtnTestInspect.IsEnabled = !isProcessing;
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
            FileLogger.Instance.Log(message);
        }

        private void BtnOpenLogDir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string logDir = Path.Combine(baseDir, "logs");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = logDir,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"로그 폴더 열기 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
