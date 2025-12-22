using System.Windows.Input;
using RukiCheck.Services;

namespace RukiCheck.ViewModels;

/// <summary>
/// トラックパッド検査ウィンドウのViewModel
/// </summary>
public class TrackpadTestViewModel : ViewModelBase
{
    private readonly TrackpadInspectionService _service;

    private bool _cursorMoved = false;
    private bool _leftClicked = false;
    private bool _rightClicked = false;
    private bool _scrollDetected = false;
    private string _statusMessage = "トラックパッドを操作してください";
    private int _completedTests = 0;

    public TrackpadTestViewModel(TrackpadInspectionService service)
    {
        _service = service;

        // コマンド
        CompleteCommand = new RelayCommand(_ => OnComplete(), _ => CanComplete());
        ResetCommand = new RelayCommand(_ => OnReset());
    }

    #region Properties

    public bool CursorMoved
    {
        get => _cursorMoved;
        set
        {
            if (SetProperty(ref _cursorMoved, value))
            {
                UpdateProgress();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool LeftClicked
    {
        get => _leftClicked;
        set
        {
            if (SetProperty(ref _leftClicked, value))
            {
                UpdateProgress();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool RightClicked
    {
        get => _rightClicked;
        set
        {
            if (SetProperty(ref _rightClicked, value))
            {
                UpdateProgress();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool ScrollDetected
    {
        get => _scrollDetected;
        set
        {
            if (SetProperty(ref _scrollDetected, value))
            {
                UpdateProgress();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public int CompletedTests
    {
        get => _completedTests;
        set => SetProperty(ref _completedTests, value);
    }

    #endregion

    #region Commands

    public RelayCommand CompleteCommand { get; }
    public RelayCommand ResetCommand { get; }

    public Action? OnCompleted { get; set; }

    #endregion

    #region Methods

    /// <summary>
    /// カーソル移動を処理
    /// </summary>
    public void HandleMouseMove()
    {
        if (!CursorMoved)
        {
            CursorMoved = true;
            _service.RecordCursorMove();
        }
    }

    /// <summary>
    /// 左クリックを処理
    /// </summary>
    public void HandleLeftClick()
    {
        if (!LeftClicked)
        {
            LeftClicked = true;
            _service.RecordLeftClick();
        }
    }

    /// <summary>
    /// 右クリックを処理
    /// </summary>
    public void HandleRightClick()
    {
        if (!RightClicked)
        {
            RightClicked = true;
            _service.RecordRightClick();
        }
    }

    /// <summary>
    /// スクロールを処理
    /// </summary>
    public void HandleScroll()
    {
        if (!ScrollDetected)
        {
            ScrollDetected = true;
            _service.RecordScroll();
        }
    }

    private void UpdateProgress()
    {
        CompletedTests = 0;
        if (CursorMoved) CompletedTests++;
        if (LeftClicked) CompletedTests++;
        if (RightClicked) CompletedTests++;
        if (ScrollDetected) CompletedTests++;

        StatusMessage = CompletedTests == 4
            ? "🎉 すべてのテストが完了しました！「完了」ボタンを押してください"
            : $"進捗: {CompletedTests} / 4 項目完了";
    }

    private bool CanComplete()
    {
        return CompletedTests >= 2; // 最低2項目完了で完了可能
    }

    private void OnComplete()
    {
        OnCompleted?.Invoke();
    }

    private void OnReset()
    {
        _service.Reset();
        CursorMoved = false;
        LeftClicked = false;
        RightClicked = false;
        ScrollDetected = false;
        StatusMessage = "リセットしました。トラックパッドを操作してください";
    }

    #endregion
}
