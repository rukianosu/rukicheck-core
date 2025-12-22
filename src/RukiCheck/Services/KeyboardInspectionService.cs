using RukiCheck.Models;

namespace RukiCheck.Services;

/// <summary>
/// キーボード検査サービス
/// UIから押下されたキー情報を受け取って検証する
/// </summary>
public class KeyboardInspectionService : IInspectionService<KeyboardResult>
{
    private readonly HashSet<string> _pressedKeys = new();
    private string _layout = "JIS";

    /// <summary>
    /// キーボードレイアウトを設定
    /// </summary>
    public void SetLayout(string layout)
    {
        _layout = layout;
    }

    /// <summary>
    /// 押下されたキーを記録
    /// </summary>
    public void RecordKeyPress(string keyName)
    {
        _pressedKeys.Add(keyName);
    }

    /// <summary>
    /// 検査を完了して結果を生成
    /// </summary>
    public Task<KeyboardResult> ExecuteAsync(string attachmentPath)
    {
        var result = new KeyboardResult
        {
            Layout = _layout
        };

        try
        {
            // 期待キー数（JIS配列の標準キー数）
            var expectedKeys = GetExpectedKeys(_layout);

            result.TotalKeys = expectedKeys.Count;
            result.PressedKeys = _pressedKeys.Count;

            // 未入力キーを検出
            result.MissingKeys = expectedKeys.Except(_pressedKeys).ToList();

            // 判定
            if (result.MissingKeys.Count == 0)
            {
                result.Result = "pass";
            }
            else if (result.MissingKeys.Count <= 3)
            {
                result.Result = "warn";
                result.Note = $"一部のキーが未入力です: {string.Join(", ", result.MissingKeys)}";
            }
            else
            {
                result.Result = "fail";
                result.Note = $"{result.MissingKeys.Count}個のキーが未入力です";
            }
        }
        catch (Exception ex)
        {
            result.Error = $"検査失敗: {ex.Message}";
            result.Result = "error";
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// レイアウトに応じた期待キーセットを取得
    /// </summary>
    private HashSet<string> GetExpectedKeys(string layout)
    {
        // JIS配列の主要キー（109キー）
        var keys = new HashSet<string>
        {
            // 数字行
            "Escape", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
            "PrintScreen", "Scroll", "Pause",

            // メイン文字キー
            "Oem3", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9", "D0", "OemMinus", "OemPlus", "Back",
            "Tab", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "Oem4", "Oem6", "Return",
            "CapsLock", "A", "S", "D", "F", "G", "H", "J", "K", "L", "Oem1", "Oem7", "Oem5",
            "LeftShift", "Z", "X", "C", "V", "B", "N", "M", "OemComma", "OemPeriod", "Oem2", "RightShift",

            // 修飾キー
            "LeftCtrl", "LWin", "LeftAlt", "Space", "RightAlt", "RWin", "Apps", "RightCtrl",

            // 編集キー
            "Insert", "Delete", "Home", "End", "PageUp", "PageDown",

            // 矢印キー
            "Left", "Up", "Down", "Right",

            // テンキー
            "NumLock", "Divide", "Multiply", "Subtract",
            "NumPad7", "NumPad8", "NumPad9", "Add",
            "NumPad4", "NumPad5", "NumPad6",
            "NumPad1", "NumPad2", "NumPad3", "Return",
            "NumPad0", "Decimal"
        };

        // US配列の場合の差分（将来対応）
        if (layout == "US")
        {
            // JIS固有キー削除など
            // keys.Remove("Oem5"); // バックスラッシュ位置違い
        }

        // Fnキーは対象外（仕様）
        return keys;
    }

    /// <summary>
    /// 検査状態をリセット
    /// </summary>
    public void Reset()
    {
        _pressedKeys.Clear();
    }
}
