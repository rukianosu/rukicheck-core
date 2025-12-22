using System.Windows;
using RukiCheck.Services;

namespace RukiCheck.ViewModels;

/// <summary>
/// マイク検査ウィンドウのViewModel
/// </summary>
public class MicrophoneTestViewModel : ViewModelBase
{
    private readonly MicrophoneInspectionService _service;

    private bool _isRecording = false;
    private bool _isRecorded = false;
    private bool _isPlaying = false;
    private int _recordingProgress = 0;
    private string _statusMessage = "「録音開始」ボタンを押してください";
    private string _recordedFilePath = string.Empty;
    private bool? _userConfirmed = null;

    public MicrophoneTestViewModel(MicrophoneInspectionService service)
    {
        _service = service;

        // コマンド
        StartRecordingCommand = new AsyncRelayCommand(async _ => await StartRecordingAsync(), _ => !IsRecording && !IsRecorded);
        PlayRecordingCommand = new AsyncRelayCommand(async _ => await PlayRecordingAsync(), _ => IsRecorded && !IsPlaying);
        ConfirmYesCommand = new RelayCommand(_ => ConfirmYes(), _ => IsRecorded);
        ConfirmNoCommand = new RelayCommand(_ => ConfirmNo(), _ => IsRecorded);
        ResetCommand = new RelayCommand(_ => Reset());
    }

    #region Properties

    public bool IsRecording
    {
        get => _isRecording;
        set => SetProperty(ref _isRecording, value);
    }

    public bool IsRecorded
    {
        get => _isRecorded;
        set => SetProperty(ref _isRecorded, value);
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set => SetProperty(ref _isPlaying, value);
    }

    public int RecordingProgress
    {
        get => _recordingProgress;
        set => SetProperty(ref _recordingProgress, value);
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

    public AsyncRelayCommand StartRecordingCommand { get; }
    public AsyncRelayCommand PlayRecordingCommand { get; }
    public RelayCommand ConfirmYesCommand { get; }
    public RelayCommand ConfirmNoCommand { get; }
    public RelayCommand ResetCommand { get; }

    public Action? OnCompleted { get; set; }

    #endregion

    #region Methods

    private async Task StartRecordingAsync()
    {
        try
        {
            IsRecording = true;
            StatusMessage = "録音中... (5秒間)";

            // プログレスバー更新
            for (int i = 0; i <= 100; i += 2)
            {
                RecordingProgress = i;
                await Task.Delay(100); // 5秒 = 5000ms / 50回 = 100ms
            }

            // 録音実行
            _recordedFilePath = await _service.StartRecordingAsync();

            IsRecording = false;
            IsRecorded = true;
            RecordingProgress = 100;
            StatusMessage = "録音完了！「再生」ボタンで確認してください";
        }
        catch (Exception ex)
        {
            IsRecording = false;
            StatusMessage = $"録音エラー: {ex.Message}";
            MessageBox.Show($"録音に失敗しました:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task PlayRecordingAsync()
    {
        try
        {
            IsPlaying = true;
            StatusMessage = "再生中...";

            await _service.PlayRecordingAsync(_recordedFilePath);

            IsPlaying = false;
            StatusMessage = "再生完了。音が聞こえましたか？";
        }
        catch (Exception ex)
        {
            IsPlaying = false;
            StatusMessage = $"再生エラー: {ex.Message}";
            MessageBox.Show($"再生に失敗しました:\n{ex.Message}", "エラー",
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
        StatusMessage = "❌ マイクに問題がある可能性があります";
        OnCompleted?.Invoke();
    }

    private void Reset()
    {
        IsRecording = false;
        IsRecorded = false;
        IsPlaying = false;
        RecordingProgress = 0;
        UserConfirmed = null;
        StatusMessage = "「録音開始」ボタンを押してください";
    }

    #endregion
}
