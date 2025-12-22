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

    private string _managementId = string.Empty;
    private string _inspectionDate;
    private string _savePath = string.Empty;
    private string _statusMessage = "管理番号と保存先を入力してください";
    private bool _isInspectionStarted = false;

    private InspectionSession? _session;

    public MainViewModel(
        InspectionOrchestrator orchestrator,
        StorageInspectionService storageService,
        CpuInspectionService cpuService)
    {
        _orchestrator = orchestrator;
        _storageService = storageService;
        _cpuService = cpuService;

        // 日付を今日で固定
        _inspectionDate = DateTime.Now.ToString("yyyy年MM月dd日");

        // コマンド初期化
        SelectSavePathCommand = new RelayCommand(_ => SelectSavePath());
        StartInspectionCommand = new RelayCommand(_ => StartInspection(), _ => CanStartInspection());
        RunStorageTestCommand = new AsyncRelayCommand(async _ => await RunStorageTestAsync());
        RunKeyboardTestCommand = new RelayCommand(_ => RunKeyboardTest());
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
    public AsyncRelayCommand RunStorageTestCommand { get; }
    public RelayCommand RunKeyboardTestCommand { get; }
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

    private void StartInspection()
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
            StatusMessage = $"検品開始: {_session.OutputPath}";

            MessageBox.Show($"検品フォルダを作成しました:\n{_session.OutputPath}",
                "検品開始", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"検品の開始に失敗しました:\n{ex.Message}",
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
