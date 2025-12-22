using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RukiCheck.Models;

/// <summary>
/// キーボードの1つのキー情報
/// </summary>
public class KeyInfo : INotifyPropertyChanged
{
    private bool _isPressed = false;

    /// <summary>
    /// WPFのKey Enum値（例: "A", "D1", "NumPad0"）
    /// </summary>
    public string KeyCode { get; set; } = string.Empty;

    /// <summary>
    /// 表示名（例: "A", "1", "Enter"）
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 押下済みフラグ
    /// </summary>
    public bool IsPressed
    {
        get => _isPressed;
        set
        {
            if (_isPressed != value)
            {
                _isPressed = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// キーの横幅（通常は1.0、Enterキー等は大きい）
    /// </summary>
    public double Width { get; set; } = 1.0;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
