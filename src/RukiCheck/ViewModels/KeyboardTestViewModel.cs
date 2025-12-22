using System.Collections.ObjectModel;
using System.Windows.Input;
using RukiCheck.Models;
using RukiCheck.Services;

namespace RukiCheck.ViewModels;

/// <summary>
/// キーボード検査ウィンドウのViewModel
/// </summary>
public class KeyboardTestViewModel : ViewModelBase
{
    private readonly KeyboardInspectionService _service;
    private string _layout = "JIS";
    private int _totalKeys = 0;
    private int _pressedCount = 0;
    private double _progress = 0;
    private string _statusMessage = "すべてのキーを1回ずつ押してください";

    public KeyboardTestViewModel(KeyboardInspectionService service)
    {
        _service = service;

        // キー配列を初期化
        InitializeKeys();

        // コマンド
        CompleteCommand = new RelayCommand(_ => OnComplete());
        ResetCommand = new RelayCommand(_ => OnReset());
    }

    #region Properties

    public ObservableCollection<ObservableCollection<KeyInfo>> KeyRows { get; } = new();

    public string Layout
    {
        get => _layout;
        set
        {
            if (SetProperty(ref _layout, value))
            {
                _service.SetLayout(value);
                InitializeKeys(); // レイアウト変更時に再初期化
            }
        }
    }

    public int TotalKeys
    {
        get => _totalKeys;
        set => SetProperty(ref _totalKeys, value);
    }

    public int PressedCount
    {
        get => _pressedCount;
        set
        {
            if (SetProperty(ref _pressedCount, value))
            {
                Progress = _totalKeys > 0 ? (double)_pressedCount / _totalKeys * 100 : 0;
                UpdateStatusMessage();
            }
        }
    }

    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
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

    /// <summary>
    /// キー押下を処理
    /// </summary>
    public void HandleKeyPress(Key key)
    {
        var keyName = key.ToString();

        // すでに押されているかチェック
        bool alreadyPressed = false;
        foreach (var row in KeyRows)
        {
            var keyInfo = row.FirstOrDefault(k => k.KeyCode == keyName);
            if (keyInfo != null)
            {
                if (keyInfo.IsPressed)
                {
                    alreadyPressed = true;
                }
                else
                {
                    keyInfo.IsPressed = true;
                    _service.RecordKeyPress(keyName);
                    PressedCount++;
                }
                break;
            }
        }

        if (!alreadyPressed && PressedCount == TotalKeys)
        {
            StatusMessage = "🎉 すべてのキーが押されました！「完了」ボタンを押してください";
        }
    }

    private void InitializeKeys()
    {
        KeyRows.Clear();
        PressedCount = 0;

        // JIS配列のキー定義
        if (_layout == "JIS")
        {
            InitializeJISLayout();
        }
        else
        {
            InitializeUSLayout();
        }

        TotalKeys = KeyRows.Sum(row => row.Count);
    }

    private void InitializeJISLayout()
    {
        // 第1列: Escキー + ファンクションキー
        KeyRows.Add(new ObservableCollection<KeyInfo>
        {
            new() { KeyCode = "Escape", DisplayName = "Esc", Width = 1.0 },
            new() { KeyCode = "F1", DisplayName = "F1", Width = 1.0 },
            new() { KeyCode = "F2", DisplayName = "F2", Width = 1.0 },
            new() { KeyCode = "F3", DisplayName = "F3", Width = 1.0 },
            new() { KeyCode = "F4", DisplayName = "F4", Width = 1.0 },
            new() { KeyCode = "F5", DisplayName = "F5", Width = 1.0 },
            new() { KeyCode = "F6", DisplayName = "F6", Width = 1.0 },
            new() { KeyCode = "F7", DisplayName = "F7", Width = 1.0 },
            new() { KeyCode = "F8", DisplayName = "F8", Width = 1.0 },
            new() { KeyCode = "F9", DisplayName = "F9", Width = 1.0 },
            new() { KeyCode = "F10", DisplayName = "F10", Width = 1.0 },
            new() { KeyCode = "F11", DisplayName = "F11", Width = 1.0 },
            new() { KeyCode = "F12", DisplayName = "F12", Width = 1.0 },
        });

        // 第2列: 数字行
        KeyRows.Add(new ObservableCollection<KeyInfo>
        {
            new() { KeyCode = "Oem3", DisplayName = "`", Width = 1.0 },
            new() { KeyCode = "D1", DisplayName = "1", Width = 1.0 },
            new() { KeyCode = "D2", DisplayName = "2", Width = 1.0 },
            new() { KeyCode = "D3", DisplayName = "3", Width = 1.0 },
            new() { KeyCode = "D4", DisplayName = "4", Width = 1.0 },
            new() { KeyCode = "D5", DisplayName = "5", Width = 1.0 },
            new() { KeyCode = "D6", DisplayName = "6", Width = 1.0 },
            new() { KeyCode = "D7", DisplayName = "7", Width = 1.0 },
            new() { KeyCode = "D8", DisplayName = "8", Width = 1.0 },
            new() { KeyCode = "D9", DisplayName = "9", Width = 1.0 },
            new() { KeyCode = "D0", DisplayName = "0", Width = 1.0 },
            new() { KeyCode = "OemMinus", DisplayName = "-", Width = 1.0 },
            new() { KeyCode = "OemPlus", DisplayName = "=", Width = 1.0 },
            new() { KeyCode = "Back", DisplayName = "Back", Width = 2.0 },
        });

        // 第3列: QWERTY行
        KeyRows.Add(new ObservableCollection<KeyInfo>
        {
            new() { KeyCode = "Tab", DisplayName = "Tab", Width = 1.5 },
            new() { KeyCode = "Q", DisplayName = "Q", Width = 1.0 },
            new() { KeyCode = "W", DisplayName = "W", Width = 1.0 },
            new() { KeyCode = "E", DisplayName = "E", Width = 1.0 },
            new() { KeyCode = "R", DisplayName = "R", Width = 1.0 },
            new() { KeyCode = "T", DisplayName = "T", Width = 1.0 },
            new() { KeyCode = "Y", DisplayName = "Y", Width = 1.0 },
            new() { KeyCode = "U", DisplayName = "U", Width = 1.0 },
            new() { KeyCode = "I", DisplayName = "I", Width = 1.0 },
            new() { KeyCode = "O", DisplayName = "O", Width = 1.0 },
            new() { KeyCode = "P", DisplayName = "P", Width = 1.0 },
            new() { KeyCode = "Oem4", DisplayName = "[", Width = 1.0 },
            new() { KeyCode = "Oem6", DisplayName = "]", Width = 1.0 },
            new() { KeyCode = "Return", DisplayName = "Enter", Width = 1.5 },
        });

        // 第4列: ASDF行
        KeyRows.Add(new ObservableCollection<KeyInfo>
        {
            new() { KeyCode = "CapsLock", DisplayName = "Caps", Width = 1.8 },
            new() { KeyCode = "A", DisplayName = "A", Width = 1.0 },
            new() { KeyCode = "S", DisplayName = "S", Width = 1.0 },
            new() { KeyCode = "D", DisplayName = "D", Width = 1.0 },
            new() { KeyCode = "F", DisplayName = "F", Width = 1.0 },
            new() { KeyCode = "G", DisplayName = "G", Width = 1.0 },
            new() { KeyCode = "H", DisplayName = "H", Width = 1.0 },
            new() { KeyCode = "J", DisplayName = "J", Width = 1.0 },
            new() { KeyCode = "K", DisplayName = "K", Width = 1.0 },
            new() { KeyCode = "L", DisplayName = "L", Width = 1.0 },
            new() { KeyCode = "Oem1", DisplayName = ";", Width = 1.0 },
            new() { KeyCode = "Oem7", DisplayName = "'", Width = 1.0 },
            new() { KeyCode = "Oem5", DisplayName = "\\", Width = 1.2 },
        });

        // 第5列: ZXCV行
        KeyRows.Add(new ObservableCollection<KeyInfo>
        {
            new() { KeyCode = "LeftShift", DisplayName = "Shift", Width = 2.3 },
            new() { KeyCode = "Z", DisplayName = "Z", Width = 1.0 },
            new() { KeyCode = "X", DisplayName = "X", Width = 1.0 },
            new() { KeyCode = "C", DisplayName = "C", Width = 1.0 },
            new() { KeyCode = "V", DisplayName = "V", Width = 1.0 },
            new() { KeyCode = "B", DisplayName = "B", Width = 1.0 },
            new() { KeyCode = "N", DisplayName = "N", Width = 1.0 },
            new() { KeyCode = "M", DisplayName = "M", Width = 1.0 },
            new() { KeyCode = "OemComma", DisplayName = ",", Width = 1.0 },
            new() { KeyCode = "OemPeriod", DisplayName = ".", Width = 1.0 },
            new() { KeyCode = "Oem2", DisplayName = "/", Width = 1.0 },
            new() { KeyCode = "RightShift", DisplayName = "Shift", Width = 2.7 },
        });

        // 第6列: スペースバー行
        KeyRows.Add(new ObservableCollection<KeyInfo>
        {
            new() { KeyCode = "LeftCtrl", DisplayName = "Ctrl", Width = 1.5 },
            new() { KeyCode = "LWin", DisplayName = "Win", Width = 1.2 },
            new() { KeyCode = "LeftAlt", DisplayName = "Alt", Width = 1.2 },
            new() { KeyCode = "Space", DisplayName = "Space", Width = 6.0 },
            new() { KeyCode = "RightAlt", DisplayName = "Alt", Width = 1.2 },
            new() { KeyCode = "RWin", DisplayName = "Win", Width = 1.2 },
            new() { KeyCode = "Apps", DisplayName = "Menu", Width = 1.2 },
            new() { KeyCode = "RightCtrl", DisplayName = "Ctrl", Width = 1.5 },
        });

        // 第7列: 矢印キー
        KeyRows.Add(new ObservableCollection<KeyInfo>
        {
            new() { KeyCode = "Left", DisplayName = "←", Width = 1.0 },
            new() { KeyCode = "Up", DisplayName = "↑", Width = 1.0 },
            new() { KeyCode = "Down", DisplayName = "↓", Width = 1.0 },
            new() { KeyCode = "Right", DisplayName = "→", Width = 1.0 },
        });

        // 第8列: 編集キー
        KeyRows.Add(new ObservableCollection<KeyInfo>
        {
            new() { KeyCode = "Insert", DisplayName = "Ins", Width = 1.0 },
            new() { KeyCode = "Delete", DisplayName = "Del", Width = 1.0 },
            new() { KeyCode = "Home", DisplayName = "Home", Width = 1.0 },
            new() { KeyCode = "End", DisplayName = "End", Width = 1.0 },
            new() { KeyCode = "PageUp", DisplayName = "PgUp", Width = 1.0 },
            new() { KeyCode = "PageDown", DisplayName = "PgDn", Width = 1.0 },
        });

        // 第9列: テンキー
        KeyRows.Add(new ObservableCollection<KeyInfo>
        {
            new() { KeyCode = "NumLock", DisplayName = "Num", Width = 1.0 },
            new() { KeyCode = "Divide", DisplayName = "/", Width = 1.0 },
            new() { KeyCode = "Multiply", DisplayName = "*", Width = 1.0 },
            new() { KeyCode = "Subtract", DisplayName = "-", Width = 1.0 },
            new() { KeyCode = "NumPad7", DisplayName = "7", Width = 1.0 },
            new() { KeyCode = "NumPad8", DisplayName = "8", Width = 1.0 },
            new() { KeyCode = "NumPad9", DisplayName = "9", Width = 1.0 },
            new() { KeyCode = "Add", DisplayName = "+", Width = 1.0 },
            new() { KeyCode = "NumPad4", DisplayName = "4", Width = 1.0 },
            new() { KeyCode = "NumPad5", DisplayName = "5", Width = 1.0 },
            new() { KeyCode = "NumPad6", DisplayName = "6", Width = 1.0 },
            new() { KeyCode = "NumPad1", DisplayName = "1", Width = 1.0 },
            new() { KeyCode = "NumPad2", DisplayName = "2", Width = 1.0 },
            new() { KeyCode = "NumPad3", DisplayName = "3", Width = 1.0 },
            new() { KeyCode = "NumPad0", DisplayName = "0", Width = 2.0 },
            new() { KeyCode = "Decimal", DisplayName = ".", Width = 1.0 },
        });
    }

    private void InitializeUSLayout()
    {
        // US配列は将来実装
        InitializeJISLayout();
    }

    private void UpdateStatusMessage()
    {
        if (PressedCount == 0)
        {
            StatusMessage = "すべてのキーを1回ずつ押してください";
        }
        else if (PressedCount < TotalKeys)
        {
            var remaining = TotalKeys - PressedCount;
            StatusMessage = $"残り {remaining} キー";
        }
        else
        {
            StatusMessage = "🎉 すべてのキーが押されました！「完了」ボタンを押してください";
        }
    }

    private void OnComplete()
    {
        OnCompleted?.Invoke();
    }

    private void OnReset()
    {
        _service.Reset();
        InitializeKeys();
        StatusMessage = "リセットしました。すべてのキーを1回ずつ押してください";
    }

    #endregion
}
