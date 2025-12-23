using System.Windows;
using System.Windows.Input;
using RukiCheck.Services;

namespace RukiCheck.ViewModels;

/// <summary>
/// タッチスクリーンテストウィンドウのViewModel
/// </summary>
public class TouchScreenTestViewModel : ViewModelBase
{
    private readonly TouchScreenInspectionService _service;

    private bool _touchAvailable = false;
    private string? _deviceName = null;
    private int _maxTouchPoints = 0;
    private bool _multiTouchSupported = false;
    private bool _tapDetected = false;
    private bool _swipeDetected = false;
    private bool _testCompleted = false;
    private string _statusMessage = "タッチスクリーンを検出しています...";
    private Point? _touchStartPoint = null;

    public TouchScreenTestViewModel(TouchScreenInspectionService service)
    {
        _service = service;

        // 初期検出
        DetectTouchScreen();

        // コマンド
        CompleteCommand = new RelayCommand(_ => OnComplete(), _ => CanComplete());
        ResetCommand = new RelayCommand(_ => Reset());
    }

    #region Properties

    public bool TouchAvailable
    {
        get => _touchAvailable;
        set => SetProperty(ref _touchAvailable, value);
    }

    public string? DeviceName
    {
        get => _deviceName;
        set => SetProperty(ref _deviceName, value);
    }

    public int MaxTouchPoints
    {
        get => _maxTouchPoints;
        set => SetProperty(ref _maxTouchPoints, value);
    }

    public bool MultiTouchSupported
    {
        get => _multiTouchSupported;
        set => SetProperty(ref _multiTouchSupported, value);
    }

    public bool TapDetected
    {
        get => _tapDetected;
        set
        {
            if (SetProperty(ref _tapDetected, value))
            {
                _service.SetTapDetected(value);
                UpdateStatus();
            }
        }
    }

    public bool SwipeDetected
    {
        get => _swipeDetected;
        set
        {
            if (SetProperty(ref _swipeDetected, value))
            {
                _service.SetSwipeDetected(value);
                UpdateStatus();
            }
        }
    }

    public bool TestCompleted
    {
        get => _testCompleted;
        set
        {
            if (SetProperty(ref _testCompleted, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    #endregion

    #region Commands

    public RelayCommand CompleteCommand { get; }
    public RelayCommand ResetCommand { get; }

    public Action? OnCompleted { get; set; }

    #endregion

    #region Methods

    private void DetectTouchScreen()
    {
        TouchAvailable = _service.IsTouchScreenAvailable();
        MaxTouchPoints = _service.GetMaxTouchPoints();
        MultiTouchSupported = _service.IsMultiTouchSupported();
        DeviceName = _service.GetTouchDeviceName();

        if (TouchAvailable)
        {
            StatusMessage = "タッチスクリーンが検出されました。画面をタップまたはスワイプしてテストしてください";
        }
        else
        {
            StatusMessage = "タッチスクリーンが検出されませんでした";
            TestCompleted = true;
        }
    }

    /// <summary>
    /// タッチダウンイベントハンドラー
    /// </summary>
    public void OnTouchDown(Point position)
    {
        _touchStartPoint = position;
        TapDetected = true;
    }

    /// <summary>
    /// タッチアップイベントハンドラー
    /// </summary>
    public void OnTouchUp(Point position)
    {
        // スワイプ検出（開始位置から50ピクセル以上移動）
        if (_touchStartPoint.HasValue)
        {
            double distance = Math.Sqrt(
                Math.Pow(position.X - _touchStartPoint.Value.X, 2) +
                Math.Pow(position.Y - _touchStartPoint.Value.Y, 2));

            if (distance > 50)
            {
                SwipeDetected = true;
            }

            _touchStartPoint = null;
        }

        CheckTestCompletion();
    }

    private void CheckTestCompletion()
    {
        if (TapDetected && SwipeDetected)
        {
            TestCompleted = true;
            UpdateStatus();
        }
    }

    private void UpdateStatus()
    {
        if (!TouchAvailable)
        {
            StatusMessage = "タッチスクリーンが検出されませんでした";
        }
        else if (TapDetected && SwipeDetected)
        {
            StatusMessage = "✅ すべてのテストが完了しました！「完了」ボタンを押してください";
        }
        else if (TapDetected)
        {
            StatusMessage = "タップが検出されました。次に画面をスワイプ（指でなぞる）してください";
        }
        else if (SwipeDetected)
        {
            StatusMessage = "スワイプが検出されました。次に画面をタップしてください";
        }
        else
        {
            StatusMessage = "画面をタップまたはスワイプしてテストしてください";
        }
    }

    private bool CanComplete()
    {
        return TestCompleted;
    }

    private void OnComplete()
    {
        OnCompleted?.Invoke();
    }

    private void Reset()
    {
        TapDetected = false;
        SwipeDetected = false;
        TestCompleted = false;
        _touchStartPoint = null;
        DetectTouchScreen();
    }

    #endregion
}
