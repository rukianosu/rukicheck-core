using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using RukiCheck.Application;
using RukiCheck.Models;
using RukiCheck.Services;

namespace RukiCheck.ViewModels;

/// <summary>
/// メインウィンドウのViewModel
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly InspectionOrchestrator _orchestrator;
    private readonly StorageInspectionService _storageService;
    private readonly CpuInspectionService _cpuService;
    private readonly HardwareInfoService _hardwareService;

    private string _managementId = string.Empty;
    private string _inspectionDate;
    private string _savePath = string.Empty;
    private string _statusMessage = "管理番号と保存先を入力してください";
    private bool _isInspectionStarted = false;

    private InspectionSession? _session;

    public MainViewModel(
        InspectionOrchestrator orchestrator,
        StorageInspectionService storageService,
        CpuInspectionService cpuService,
        HardwareInfoService hardwareService)
    {
        _orchestrator = orchestrator;
        _storageService = storageService;
        _cpuService = cpuService;
        _hardwareService = hardwareService;

        // 日付を今日で固定
        _inspectionDate = DateTime.Now.ToString("yyyy年MM月dd日");

        // コマンド初期化
        SelectSavePathCommand = new RelayCommand(_ => SelectSavePath());
        StartInspectionCommand = new RelayCommand(_ => StartInspection(), _ => CanStartInspection());
        AutoInspectCommand = new AsyncRelayCommand(async _ => await AutoInspectAsync(), _ => IsInspectionStarted);
        RunStorageTestCommand = new AsyncRelayCommand(async _ => await RunStorageTestAsync());
        RunKeyboardTestCommand = new RelayCommand(_ => RunKeyboardTest());
        RunMicrophoneTestCommand = new RelayCommand(_ => RunMicrophoneTest());
        RunSpeakerTestCommand = new RelayCommand(_ => RunSpeakerTest());
        RunCameraTestCommand = new RelayCommand(_ => RunCameraTest());
        RunTrackpadTestCommand = new RelayCommand(_ => RunTrackpadTest());
        RunCpuTestCommand = new AsyncRelayCommand(async _ => await RunCpuTestAsync());
        SaveReportCommand = new AsyncRelayCommand(async _ => await SaveReportAsync());
        OpenReportFolderCommand = new RelayCommand(_ => OpenReportFolder());
    }

    #region Properties

    public string ManagementId
    {
        get => _managementId;
        set => SetProperty(ref _managementId, value);
    }

    public string InspectionDate
    {
        get => _inspectionDate;
        set => SetProperty(ref _inspectionDate, value);
    }

    public string SavePath
    {
        get => _savePath;
        set => SetProperty(ref _savePath, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsInspectionStarted
    {
        get => _isInspectionStarted;
        set => SetProperty(ref _isInspectionStarted, value);
    }

    #endregion

    #region Commands

    public RelayCommand SelectSavePathCommand { get; }
    public RelayCommand StartInspectionCommand { get; }
    public AsyncRelayCommand AutoInspectCommand { get; }
    public AsyncRelayCommand RunStorageTestCommand { get; }
    public RelayCommand RunKeyboardTestCommand { get; }
    public RelayCommand RunMicrophoneTestCommand { get; }
    public RelayCommand RunSpeakerTestCommand { get; }
    public RelayCommand RunCameraTestCommand { get; }
    public RelayCommand RunTrackpadTestCommand { get; }
    public AsyncRelayCommand RunCpuTestCommand { get; }
    public AsyncRelayCommand SaveReportCommand { get; }
    public RelayCommand OpenReportFolderCommand { get; }

    #endregion

    #region Methods

    private void SelectSavePath()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "保存先フォルダを選択"
        };

        if (dialog.ShowDialog() == true)
        {
            SavePath = dialog.FolderName;
        }
    }

    private bool CanStartInspection()
    {
        return !string.IsNullOrWhiteSpace(ManagementId) &&
               !string.IsNullOrWhiteSpace(SavePath) &&
               !IsInspectionStarted;
    }

    private async void StartInspection()
    {
        try
        {
            // 書き込み権限チェック
            if (!_orchestrator.CanWriteToPath(SavePath))
            {
                MessageBox.Show("保存先に書き込み権限がありません。別のフォルダを選択してください。",
                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // セッション作成
            _session = _orchestrator.CreateSession(ManagementId, SavePath);

            IsInspectionStarted = true;
            StatusMessage = "ハードウェア情報を収集しています...";

            // ハードウェア情報を自動収集
            try
            {
                var hardwareInfo = await _hardwareService.ExecuteAsync(_session.AttachmentsPath);
                _session.Report.Hardware = hardwareInfo;

                StatusMessage = $"検品開始: {_session.OutputPath} (ハードウェア情報: CPU={hardwareInfo.Cpu.Name}, メモリ={hardwareInfo.Memory.TotalGb}GB {hardwareInfo.Memory.Type})";
            }
            catch (Exception hwEx)
            {
                StatusMessage = $"検品開始: {_session.OutputPath} (ハードウェア情報収集失敗: {hwEx.Message})";
            }

            MessageBox.Show($"検品フォルダを作成しました:\n{_session.OutputPath}\n\nハードウェア情報を自動収集しました。",
                "検品開始", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"検品の開始に失敗しました:\n{ex.Message}",
                "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 全検査を自動実行
    /// </summary>
    private async Task AutoInspectAsync()
    {
        if (_session == null) return;

        try
        {
            StatusMessage = "自動検査を開始します...";

            // 1. ストレージ検査（自動）
            StatusMessage = "1/7: ストレージ検査中...";
            await RunStorageTestAsync();
            await Task.Delay(500); // 短い待機時間

            // 2. キーボード検査（ウィンドウ表示）
            StatusMessage = "2/7: キーボード検査中...";
            RunKeyboardTest();
            await Task.Delay(500);

            // 3. マイク検査（ウィンドウ表示）
            StatusMessage = "3/7: マイク検査中...";
            RunMicrophoneTest();
            await Task.Delay(500);

            // 4. スピーカー検査（ウィンドウ表示）
            StatusMessage = "4/7: スピーカー検査中...";
            RunSpeakerTest();
            await Task.Delay(500);

            // 5. カメラ検査（ウィンドウ表示）
            StatusMessage = "5/7: カメラ検査中...";
            RunCameraTest();
            await Task.Delay(500);

            // 6. トラックパッド検査（ウィンドウ表示）
            StatusMessage = "6/7: トラックパッド検査中...";
            RunTrackpadTest();
            await Task.Delay(500);

            // 7. CPU検査（自動）
            StatusMessage = "7/7: CPU検査中...";
            await RunCpuTestAsync();

            StatusMessage = "✅ 全ての検査が完了しました！";
            MessageBox.Show("全ての検査が完了しました。\n\n「レポート保存」ボタンを押して、検品レポートを保存してください。",
                "自動検査完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"自動検査中にエラーが発生しました: {ex.Message}";
            MessageBox.Show($"自動検査中にエラーが発生しました:\n{ex.Message}",
                "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RunStorageTestAsync()
    {
        if (_session == null) return;

        try
        {
            StatusMessage = "ストレージ検査中...";
            var result = await _storageService.ExecuteAsync(_session.AttachmentsPath);
            _session.Report.Storage = result;
            StatusMessage = $"ストレージ検査完了: {result.Status}";

            MessageBox.Show($"結果: {result.Status}\n空き容量: {result.FreeGb:F1} GB ({result.FreePercent:F1}%)",
                "ストレージ検査完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ストレージ検査に失敗:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RunKeyboardTest()
    {
        if (_session == null) return;

        try
        {
            StatusMessage = "キーボード検査を開始します...";

            // DIからKeyboardTestViewModelを取得
            var viewModel = App.ServiceProvider?.GetService(typeof(KeyboardTestViewModel)) as KeyboardTestViewModel;
            if (viewModel == null)
            {
                MessageBox.Show("キーボード検査の初期化に失敗しました", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // キーボード検査ウィンドウを表示
            var window = new Views.KeyboardTestWindow(viewModel);
            var dialogResult = window.ShowDialog();

            if (dialogResult == true)
            {
                // 検査完了：結果をセッションに保存
                var result = viewModel.OnCompleted != null
                    ? Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                        .GetRequiredService<KeyboardInspectionService>(App.ServiceProvider!)
                        .ExecuteAsync(_session.AttachmentsPath).Result
                    : new KeyboardResult { Result = "cancelled" };

                _session.Report.Keyboard = result;
                StatusMessage = $"キーボード検査完了: {result.Result}";

                MessageBox.Show($"キーボード検査完了\n押下キー数: {result.PressedKeys} / {result.TotalKeys}",
                    "検査完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                StatusMessage = "キーボード検査がキャンセルされました";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"キーボード検査に失敗:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RunTrackpadTest()
    {
        if (_session == null) return;

        try
        {
            StatusMessage = "トラックパッド検査を開始します...";

            // DIからTrackpadTestViewModelを取得
            var viewModel = App.ServiceProvider?.GetService(typeof(TrackpadTestViewModel)) as TrackpadTestViewModel;
            if (viewModel == null)
            {
                MessageBox.Show("トラックパッド検査の初期化に失敗しました", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // トラックパッド検査ウィンドウを表示
            var window = new Views.TrackpadTestWindow(viewModel);
            var dialogResult = window.ShowDialog();

            if (dialogResult == true)
            {
                // 検査完了：結果をセッションに保存
                var service = App.ServiceProvider?.GetService(typeof(TrackpadInspectionService)) as TrackpadInspectionService;
                var result = service?.ExecuteAsync(_session.AttachmentsPath).Result ?? new TrackpadResult { Result = "error" };

                _session.Report.Trackpad = result;
                StatusMessage = $"トラックパッド検査完了: {result.Result}";

                MessageBox.Show($"トラックパッド検査完了\n結果: {result.Result}\n完了項目: {viewModel.CompletedTests}/4",
                    "検査完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                StatusMessage = "トラックパッド検査がキャンセルされました";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"トラックパッド検査に失敗:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RunMicrophoneTest()
    {
        if (_session == null) return;

        try
        {
            StatusMessage = "マイク検査を開始します...";

            // DIからMicrophoneTestViewModelを取得
            var viewModel = App.ServiceProvider?.GetService(typeof(MicrophoneTestViewModel)) as MicrophoneTestViewModel;
            if (viewModel == null)
            {
                MessageBox.Show("マイク検査の初期化に失敗しました", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // マイク検査ウィンドウを表示
            var window = new Views.MicrophoneTestWindow(viewModel);
            var dialogResult = window.ShowDialog();

            if (dialogResult == true)
            {
                // 検査完了：結果をセッションに保存
                var service = App.ServiceProvider?.GetService(typeof(MicrophoneInspectionService)) as MicrophoneInspectionService;
                var result = service?.ExecuteAsync(_session.AttachmentsPath).Result ?? new MicrophoneResult { Recorded = false };

                _session.Report.Microphone = result;
                StatusMessage = $"マイク検査完了: {(result.UserConfirmed ? "正常" : "異常")}";

                MessageBox.Show($"マイク検査完了\n録音: {(result.Recorded ? "成功" : "失敗")}\n確認: {(result.UserConfirmed ? "正常" : "異常")}",
                    "検査完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                StatusMessage = "マイク検査がキャンセルされました";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"マイク検査に失敗:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RunSpeakerTest()
    {
        if (_session == null) return;

        try
        {
            StatusMessage = "スピーカー検査を開始します...";

            // DIからSpeakerTestViewModelを取得
            var viewModel = App.ServiceProvider?.GetService(typeof(SpeakerTestViewModel)) as SpeakerTestViewModel;
            if (viewModel == null)
            {
                MessageBox.Show("スピーカー検査の初期化に失敗しました", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // スピーカー検査ウィンドウを表示
            var window = new Views.SpeakerTestWindow(viewModel);
            var dialogResult = window.ShowDialog();

            if (dialogResult == true)
            {
                // 検査完了：結果をセッションに保存
                var service = App.ServiceProvider?.GetService(typeof(SpeakerInspectionService)) as SpeakerInspectionService;
                var result = service?.ExecuteAsync(_session.AttachmentsPath).Result ?? new SpeakerResult();

                _session.Report.Speaker = result;
                StatusMessage = $"スピーカー検査完了: 左={result.Left}, 右={result.Right}";

                MessageBox.Show($"スピーカー検査完了\n左チャンネル: {(result.Left ? "正常" : "異常")}\n右チャンネル: {(result.Right ? "正常" : "異常")}",
                    "検査完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                StatusMessage = "スピーカー検査がキャンセルされました";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"スピーカー検査に失敗:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RunCameraTest()
    {
        if (_session == null) return;

        try
        {
            StatusMessage = "カメラ検査を開始します...";

            // DIからCameraTestViewModelを取得
            var viewModel = App.ServiceProvider?.GetService(typeof(CameraTestViewModel)) as CameraTestViewModel;
            if (viewModel == null)
            {
                MessageBox.Show("カメラ検査の初期化に失敗しました", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // カメラ検査ウィンドウを表示
            var window = new Views.CameraTestWindow(viewModel);
            var dialogResult = window.ShowDialog();

            if (dialogResult == true)
            {
                // 検査完了：結果をセッションに保存
                var service = App.ServiceProvider?.GetService(typeof(CameraInspectionService)) as CameraInspectionService;
                var result = service?.ExecuteAsync(_session.AttachmentsPath).Result ?? new CameraResult { Captured = false };

                _session.Report.Camera = result;
                StatusMessage = $"カメラ検査完了: {(result.Captured ? "成功" : "失敗")}";

                MessageBox.Show($"カメラ検査完了\nキャプチャ: {(result.Captured ? "成功" : "失敗")}\nデバイス: {result.DeviceName}",
                    "検査完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                StatusMessage = "カメラ検査がキャンセルされました";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"カメラ検査に失敗:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RunCpuTestAsync()
    {
        if (_session == null) return;

        try
        {
            StatusMessage = "CPU負荷テスト実行中（60秒）...";

            var result = await _cpuService.ExecuteAsync(_session.AttachmentsPath);
            _session.Report.Cpu = result;

            StatusMessage = $"CPUテスト完了: {result.StressTest}";

            MessageBox.Show($"CPUテスト完了\n異常: {(result.Abnormal ? "あり" : "なし")}",
                "CPUテスト完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"CPUテストに失敗:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task SaveReportAsync()
    {
        if (_session == null) return;

        try
        {
            StatusMessage = "レポート保存中...";
            await _orchestrator.SaveReportAsync(_session);
            StatusMessage = "レポート保存完了";

            MessageBox.Show($"レポートを保存しました:\n{_session.OutputPath}",
                "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"レポート保存に失敗:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenReportFolder()
    {
        if (_session == null || !Directory.Exists(_session.OutputPath))
        {
            MessageBox.Show("レポートフォルダが見つかりません", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        System.Diagnostics.Process.Start("explorer.exe", _session.OutputPath);
    }

    #endregion
}
