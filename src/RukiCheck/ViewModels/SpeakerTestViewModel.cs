using System.Windows.Input;
using RukiCheck.Services;

namespace RukiCheck.ViewModels;

/// <summary>
/// スピーカー検査ウィンドウのViewModel
/// </summary>
public class SpeakerTestViewModel : ViewModelBase
{
    private readonly SpeakerInspectionService _service;

    private bool _isPlayingLeft = false;
    private bool _isPlayingRight = false;
    private bool _leftTested = false;
    private bool _rightTested = false;
    private bool _leftConfirmed = false;
    private bool _rightConfirmed = false;
    private string _statusMessage = "「自動テスト」ボタンを押して左右のスピーカーをテストしてください";
    private bool _isAutoTesting = false;
    private bool _autoTestCompleted = false;

    public SpeakerTestViewModel(SpeakerInspectionService service)
    {
        _service = service;

        // コマンド
        StartAutoTestCommand = new AsyncRelayCommand(async _ => await StartAutoTestAsync(), _ => !IsAutoTesting && !AutoTestCompleted);
        PlayLeftCommand = new AsyncRelayCommand(async _ => await PlayLeftAsync(), _ => !IsPlayingLeft && !LeftTested);
        PlayRightCommand = new AsyncRelayCommand(async _ => await PlayRightAsync(), _ => !IsPlayingRight && !RightTested);
        ConfirmYesCommand = new RelayCommand(_ => ConfirmYes(), _ => AutoTestCompleted && !LeftTested && !RightTested);
        ConfirmNoCommand = new RelayCommand(_ => ConfirmNo(), _ => AutoTestCompleted && !LeftTested && !RightTested);
        ConfirmLeftYesCommand = new RelayCommand(_ => ConfirmLeft(true), _ => !LeftTested);
        ConfirmLeftNoCommand = new RelayCommand(_ => ConfirmLeft(false), _ => !LeftTested);
        ConfirmRightYesCommand = new RelayCommand(_ => ConfirmRight(true), _ => !RightTested);
        ConfirmRightNoCommand = new RelayCommand(_ => ConfirmRight(false), _ => !RightTested);
        CompleteCommand = new RelayCommand(_ => OnComplete(), _ => CanComplete());
        ResetCommand = new RelayCommand(_ => OnReset());
    }

    #region Properties

    public bool IsPlayingLeft
    {
        get => _isPlayingLeft;
        set
        {
            if (SetProperty(ref _isPlayingLeft, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool IsPlayingRight
    {
        get => _isPlayingRight;
        set
        {
            if (SetProperty(ref _isPlayingRight, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool LeftTested
    {
        get => _leftTested;
        set
        {
            if (SetProperty(ref _leftTested, value))
            {
                UpdateStatus();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool RightTested
    {
        get => _rightTested;
        set
        {
            if (SetProperty(ref _rightTested, value))
            {
                UpdateStatus();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool LeftConfirmed
    {
        get => _leftConfirmed;
        set => SetProperty(ref _leftConfirmed, value);
    }

    public bool RightConfirmed
    {
        get => _rightConfirmed;
        set => SetProperty(ref _rightConfirmed, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsAutoTesting
    {
        get => _isAutoTesting;
        set
        {
            if (SetProperty(ref _isAutoTesting, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool AutoTestCompleted
    {
        get => _autoTestCompleted;
        set
        {
            if (SetProperty(ref _autoTestCompleted, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    #endregion

    #region Commands

    public AsyncRelayCommand StartAutoTestCommand { get; }
    public AsyncRelayCommand PlayLeftCommand { get; }
    public AsyncRelayCommand PlayRightCommand { get; }
    public RelayCommand ConfirmYesCommand { get; }
    public RelayCommand ConfirmNoCommand { get; }
    public RelayCommand ConfirmLeftYesCommand { get; }
    public RelayCommand ConfirmLeftNoCommand { get; }
    public RelayCommand ConfirmRightYesCommand { get; }
    public RelayCommand ConfirmRightNoCommand { get; }
    public RelayCommand CompleteCommand { get; }
    public RelayCommand ResetCommand { get; }

    public Action? OnCompleted { get; set; }

    #endregion

    #region Methods

    private async Task StartAutoTestAsync()
    {
        try
        {
            IsAutoTesting = true;
            StatusMessage = "自動テストを開始します...";

            await _service.PlayAutoTestAsync((status) =>
            {
                StatusMessage = status;
            });

            AutoTestCompleted = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラー: {ex.Message}";
        }
        finally
        {
            IsAutoTesting = false;
        }
    }

    private void ConfirmYes()
    {
        LeftConfirmed = true;
        RightConfirmed = true;
        LeftTested = true;
        RightTested = true;
        _service.SetLeftConfirmation(true);
        _service.SetRightConfirmation(true);

        StatusMessage = "✅ 両方のスピーカー: 正常 - 「完了」ボタンを押してください";
        UpdateStatus();
    }

    private void ConfirmNo()
    {
        LeftConfirmed = false;
        RightConfirmed = false;
        LeftTested = true;
        RightTested = true;
        _service.SetLeftConfirmation(false);
        _service.SetRightConfirmation(false);

        StatusMessage = "❌ スピーカーに問題があります";
        UpdateStatus();
    }

    private async Task PlayLeftAsync()
    {
        try
        {
            IsPlayingLeft = true;
            StatusMessage = "🔊 左スピーカーから音を再生しています... (2秒間)";

            await _service.PlayLeftChannelAsync();

            StatusMessage = "左スピーカーから音が聞こえましたか？";
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラー: {ex.Message}";
        }
        finally
        {
            IsPlayingLeft = false;
        }
    }

    private async Task PlayRightAsync()
    {
        try
        {
            IsPlayingRight = true;
            StatusMessage = "🔊 右スピーカーから音を再生しています... (2秒間)";

            await _service.PlayRightChannelAsync();

            StatusMessage = "右スピーカーから音が聞こえましたか？";
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラー: {ex.Message}";
        }
        finally
        {
            IsPlayingRight = false;
        }
    }

    private void ConfirmLeft(bool confirmed)
    {
        LeftConfirmed = confirmed;
        LeftTested = true;
        _service.SetLeftConfirmation(confirmed);

        StatusMessage = confirmed
            ? "✅ 左スピーカー: 正常"
            : "❌ 左スピーカー: 音が聞こえませんでした";
    }

    private void ConfirmRight(bool confirmed)
    {
        RightConfirmed = confirmed;
        RightTested = true;
        _service.SetRightConfirmation(confirmed);

        StatusMessage = confirmed
            ? "✅ 右スピーカー: 正常"
            : "❌ 右スピーカー: 音が聞こえませんでした";
    }

    private void UpdateStatus()
    {
        if (LeftTested && RightTested)
        {
            if (LeftConfirmed && RightConfirmed)
            {
                StatusMessage = "🎉 すべてのテストが完了しました！「完了」ボタンを押してください";
            }
            else
            {
                StatusMessage = "⚠️ 一部のスピーカーで問題が検出されました";
            }
        }
        else if (LeftTested)
        {
            StatusMessage = "次は右スピーカーをテストしてください";
        }
        else if (RightTested)
        {
            StatusMessage = "次は左スピーカーをテストしてください";
        }
    }

    private bool CanComplete()
    {
        return LeftTested && RightTested;
    }

    private void OnComplete()
    {
        OnCompleted?.Invoke();
    }

    private void OnReset()
    {
        IsPlayingLeft = false;
        IsPlayingRight = false;
        LeftTested = false;
        RightTested = false;
        LeftConfirmed = false;
        RightConfirmed = false;
        IsAutoTesting = false;
        AutoTestCompleted = false;
        StatusMessage = "リセットしました。「自動テスト」ボタンを押して左右のスピーカーをテストしてください";
    }

    #endregion
}
