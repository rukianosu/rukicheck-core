using System.Windows;
using RukiCheck.Services;

namespace RukiCheck.ViewModels;

/// <summary>
/// メモリテストウィンドウのViewModel
/// </summary>
public class MemoryTestViewModel : ViewModelBase
{
    private readonly MemoryTestService _service;

    private bool _isTesting = false;
    private int _testProgress = 0;
    private string _statusMessage = "「テスト開始」ボタンを押してください";
    private int _testSizeMb = 512;
    private long _availableMb = 0;
    private double _totalGb = 0;
    private bool _testCompleted = false;
    private bool _testPassed = false;
    private int _patternsTested = 0;
    private int _failedPatterns = 0;
    private double _writeSpeed = 0;
    private double _readSpeed = 0;
    private double _testDuration = 0;
    private bool? _userConfirmed = null;

    public MemoryTestViewModel(MemoryTestService service)
    {
        _service = service;

        // サービスのイベントをサブスクライブ
        _service.OnProgressChanged += (progress) =>
        {
            TestProgress = progress;
        };

        _service.OnStatusChanged += (status) =>
        {
            StatusMessage = status;
        };

        // コマンド
        StartTestCommand = new AsyncRelayCommand(async _ => await StartTestAsync(), _ => !IsTesting && !TestCompleted);
        ConfirmYesCommand = new RelayCommand(_ => ConfirmYes(), _ => TestCompleted);
        ConfirmNoCommand = new RelayCommand(_ => ConfirmNo(), _ => TestCompleted);
        ResetCommand = new RelayCommand(_ => Reset());
    }

    #region Properties

    public bool IsTesting
    {
        get => _isTesting;
        set => SetProperty(ref _isTesting, value);
    }

    public int TestProgress
    {
        get => _testProgress;
        set => SetProperty(ref _testProgress, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public int TestSizeMb
    {
        get => _testSizeMb;
        set
        {
            if (SetProperty(ref _testSizeMb, value))
            {
                _service.SetTestSize(value);
            }
        }
    }

    public long AvailableMb
    {
        get => _availableMb;
        set => SetProperty(ref _availableMb, value);
    }

    public double TotalGb
    {
        get => _totalGb;
        set => SetProperty(ref _totalGb, value);
    }

    public bool TestCompleted
    {
        get => _testCompleted;
        set => SetProperty(ref _testCompleted, value);
    }

    public bool TestPassed
    {
        get => _testPassed;
        set => SetProperty(ref _testPassed, value);
    }

    public int PatternsTested
    {
        get => _patternsTested;
        set => SetProperty(ref _patternsTested, value);
    }

    public int FailedPatterns
    {
        get => _failedPatterns;
        set => SetProperty(ref _failedPatterns, value);
    }

    public double WriteSpeed
    {
        get => _writeSpeed;
        set => SetProperty(ref _writeSpeed, value);
    }

    public double ReadSpeed
    {
        get => _readSpeed;
        set => SetProperty(ref _readSpeed, value);
    }

    public double TestDuration
    {
        get => _testDuration;
        set => SetProperty(ref _testDuration, value);
    }

    public bool? UserConfirmed
    {
        get => _userConfirmed;
        set => SetProperty(ref _userConfirmed, value);
    }

    #endregion

    #region Commands

    public AsyncRelayCommand StartTestCommand { get; }
    public RelayCommand ConfirmYesCommand { get; }
    public RelayCommand ConfirmNoCommand { get; }
    public RelayCommand ResetCommand { get; }

    public Action? OnCompleted { get; set; }

    #endregion

    #region Methods

    private async Task StartTestAsync()
    {
        try
        {
            IsTesting = true;
            TestProgress = 0;
            StatusMessage = "メモリテストを開始します...";

            // テスト実行
            bool success = await _service.RunMemoryTestAsync();

            IsTesting = false;
            TestCompleted = true;
            TestPassed = success;

            // 結果取得
            var result = await _service.ExecuteAsync(string.Empty);
            AvailableMb = result.AvailableMb;
            TotalGb = result.TotalGb;
            PatternsTested = result.PatternsTested;
            FailedPatterns = result.FailedPatterns;
            WriteSpeed = result.WriteSpeedMbps;
            ReadSpeed = result.ReadSpeedMbps;
            TestDuration = result.TestDurationSec;
            TestSizeMb = result.TestedMb;

            if (success)
            {
                StatusMessage = $"✅ テスト成功！全{PatternsTested}パターン合格";
                MessageBox.Show(
                    $"メモリテスト成功！\n\n" +
                    $"テストサイズ: {TestSizeMb} MB\n" +
                    $"パターン: {PatternsTested}個すべて合格\n" +
                    $"書き込み速度: {WriteSpeed:F0} MB/s\n" +
                    $"読み込み速度: {ReadSpeed:F0} MB/s\n" +
                    $"実行時間: {TestDuration:F1}秒",
                    "テスト成功",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                StatusMessage = $"❌ テスト失敗: {FailedPatterns}個のパターンで不合格";
                MessageBox.Show(
                    $"メモリテストで問題が検出されました。\n\n" +
                    $"失敗パターン: {FailedPatterns}/{PatternsTested}\n\n" +
                    $"メモリに異常がある可能性があります。",
                    "テスト失敗",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            IsTesting = false;
            StatusMessage = $"エラー: {ex.Message}";
            MessageBox.Show($"メモリテストに失敗しました:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ConfirmYes()
    {
        _service.SetUserConfirmation(true);
        UserConfirmed = true;
        StatusMessage = "✅ 確認完了！「完了」ボタンを押してください";
        OnCompleted?.Invoke();
    }

    private void ConfirmNo()
    {
        _service.SetUserConfirmation(false);
        UserConfirmed = false;
        StatusMessage = "❌ メモリに問題がある可能性があります";
        OnCompleted?.Invoke();
    }

    private void Reset()
    {
        IsTesting = false;
        TestProgress = 0;
        TestCompleted = false;
        TestPassed = false;
        UserConfirmed = null;
        StatusMessage = "「テスト開始」ボタンを押してください";
        PatternsTested = 0;
        FailedPatterns = 0;
        WriteSpeed = 0;
        ReadSpeed = 0;
        TestDuration = 0;
    }

    #endregion
}
