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
    private bool _isPlayingPanning = false;
    private bool _leftTested = false;
    private bool _rightTested = false;
    private bool _leftConfirmed = false;
    private bool _rightConfirmed = false;
    private string _statusMessage = "左右のスピーカーをテストしてください";

    public SpeakerTestViewModel(SpeakerInspectionService service)
    {
        _service = service;

        // コマンド
        PlayLeftCommand = new AsyncRelayCommand(async _ => await PlayLeftAsync(), _ => !IsPlayingLeft && !LeftTested);
        PlayRightCommand = new AsyncRelayCommand(async _ => await PlayRightAsync(), _ => !IsPlayingRight && !RightTested);
        PlayPanningCommand = new AsyncRelayCommand(async _ => await PlayPanningAsync(), _ => !IsPlayingPanning);
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

    public bool IsPlayingPanning
    {
        get => _isPlayingPanning;
        set
        {
            if (SetProperty(ref _isPlayingPanning, value))
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

    #endregion

    #region Commands

    public AsyncRelayCommand PlayLeftCommand { get; }
    public AsyncRelayCommand PlayRightCommand { get; }
    public AsyncRelayCommand PlayPanningCommand { get; }
    public RelayCommand ConfirmLeftYesCommand { get; }
    public RelayCommand ConfirmLeftNoCommand { get; }
    public RelayCommand ConfirmRightYesCommand { get; }
    public RelayCommand ConfirmRightNoCommand { get; }
    public RelayCommand CompleteCommand { get; }
    public RelayCommand ResetCommand { get; }

    public Action? OnCompleted { get; set; }

    #endregion

    #region Methods

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

    private async Task PlayPanningAsync()
    {
        try
        {
            IsPlayingPanning = true;
            StatusMessage = "🔊 パンニングテスト: 音が左から右へスムーズに移動します... (5秒間)";

            await _service.PlayPanningTestAsync();

            StatusMessage = "パンニングテストが完了しました";
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラー: {ex.Message}";
        }
        finally
        {
            IsPlayingPanning = false;
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
        IsPlayingPanning = false;
        LeftTested = false;
        RightTested = false;
        LeftConfirmed = false;
        RightConfirmed = false;
        StatusMessage = "リセットしました。左右のスピーカーをテストしてください";
    }

    #endregion
}
