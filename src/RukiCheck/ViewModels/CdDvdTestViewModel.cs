using System.Windows;
using RukiCheck.Services;

namespace RukiCheck.ViewModels;

/// <summary>
/// CD/DVDドライブ検査ウィンドウのViewModel
/// </summary>
public class CdDvdTestViewModel : ViewModelBase
{
    private readonly CdDvdInspectionService _service;

    private bool _driveFound = false;
    private string _driveName = "検索中...";
    private string _driveLetter = string.Empty;
    private bool _mediaInserted = false;
    private string _mediaLabel = string.Empty;
    private bool _isScanning = false;
    private bool _isReading = false;
    private int _filesRead = 0;
    private bool _readTestSuccess = false;
    private string _statusMessage = "「ドライブスキャン」ボタンを押してください";
    private bool? _userConfirmed = null;

    public CdDvdTestViewModel(CdDvdInspectionService service)
    {
        _service = service;

        // コマンド
        ScanDriveCommand = new RelayCommand(_ => ScanDrive(), _ => !IsScanning);
        ReadTestCommand = new AsyncRelayCommand(async _ => await ReadTestAsync(), _ => MediaInserted && !IsReading);
        ConfirmYesCommand = new RelayCommand(_ => ConfirmYes(), _ => DriveFound);
        ConfirmNoCommand = new RelayCommand(_ => ConfirmNo(), _ => DriveFound);
    }

    #region Properties

    public bool DriveFound
    {
        get => _driveFound;
        set => SetProperty(ref _driveFound, value);
    }

    public string DriveName
    {
        get => _driveName;
        set => SetProperty(ref _driveName, value);
    }

    public string DriveLetter
    {
        get => _driveLetter;
        set => SetProperty(ref _driveLetter, value);
    }

    public bool MediaInserted
    {
        get => _mediaInserted;
        set => SetProperty(ref _mediaInserted, value);
    }

    public string MediaLabel
    {
        get => _mediaLabel;
        set => SetProperty(ref _mediaLabel, value);
    }

    public bool IsScanning
    {
        get => _isScanning;
        set => SetProperty(ref _isScanning, value);
    }

    public bool IsReading
    {
        get => _isReading;
        set => SetProperty(ref _isReading, value);
    }

    public int FilesRead
    {
        get => _filesRead;
        set => SetProperty(ref _filesRead, value);
    }

    public bool ReadTestSuccess
    {
        get => _readTestSuccess;
        set => SetProperty(ref _readTestSuccess, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool? UserConfirmed
    {
        get => _userConfirmed;
        set => SetProperty(ref _userConfirmed, value);
    }

    #endregion

    #region Commands

    public RelayCommand ScanDriveCommand { get; }
    public AsyncRelayCommand ReadTestCommand { get; }
    public RelayCommand ConfirmYesCommand { get; }
    public RelayCommand ConfirmNoCommand { get; }

    public Action? OnCompleted { get; set; }

    #endregion

    #region Methods

    private void ScanDrive()
    {
        try
        {
            IsScanning = true;
            StatusMessage = "光学ドライブをスキャン中...";

            bool found = _service.ScanOpticalDrive();
            DriveFound = found;

            if (found)
            {
                // メディア挿入状態を確認
                MediaInserted = _service.IsMediaInserted();

                // ドライブ情報はExecuteAsyncで取得されるため、ここでは簡易表示
                DriveName = "光学ドライブが見つかりました";
                StatusMessage = MediaInserted
                    ? "✅ ドライブ検出成功！メディアが挿入されています。「読み込みテスト」ボタンを押してください"
                    : "⚠️ ドライブ検出成功！メディアを挿入してください";
            }
            else
            {
                DriveName = "光学ドライブが見つかりませんでした";
                StatusMessage = "❌ 光学ドライブが見つかりませんでした";
                MessageBox.Show("光学ドライブが検出できませんでした。\nCD/DVDドライブが搭載されていない可能性があります。",
                    "ドライブ未検出", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            IsScanning = false;
        }
        catch (Exception ex)
        {
            IsScanning = false;
            StatusMessage = $"スキャンエラー: {ex.Message}";
            MessageBox.Show($"ドライブスキャンに失敗しました:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ReadTestAsync()
    {
        try
        {
            IsReading = true;
            StatusMessage = "メディアから読み込み中...";

            var (success, fileCount) = await _service.TestReadAsync();
            ReadTestSuccess = success;
            FilesRead = fileCount;

            IsReading = false;

            if (success)
            {
                StatusMessage = $"✅ 読み込みテスト成功！{fileCount}個のファイル/フォルダを検出しました";
                MessageBox.Show($"読み込みテスト成功！\n\n検出: {fileCount}個のファイル/フォルダ\n\nCD/DVDドライブは正常に動作しています。",
                    "テスト成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                StatusMessage = "❌ 読み込みテスト失敗";
                MessageBox.Show("メディアの読み込みに失敗しました。\n別のCD/DVDで再度お試しください。",
                    "テスト失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            IsReading = false;
            StatusMessage = $"読み込みエラー: {ex.Message}";
            MessageBox.Show($"読み込みテストに失敗しました:\n{ex.Message}", "エラー",
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
        StatusMessage = "❌ CD/DVDドライブに問題がある可能性があります";
        OnCompleted?.Invoke();
    }

    #endregion
}
