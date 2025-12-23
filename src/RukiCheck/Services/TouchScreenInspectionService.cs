using System.Management;
using System.Runtime.InteropServices;
using RukiCheck.Models;

namespace RukiCheck.Services;

/// <summary>
/// タッチスクリーン検査サービス
/// </summary>
public class TouchScreenInspectionService : IInspectionService<TouchScreenResult>
{
    private bool _tapDetected = false;
    private bool _swipeDetected = false;

    // Windows API定数
    private const int SM_DIGITIZER = 94;
    private const int SM_MAXIMUMTOUCHES = 95;
    private const int NID_INTEGRATED_TOUCH = 0x01;
    private const int NID_EXTERNAL_TOUCH = 0x02;
    private const int NID_MULTI_INPUT = 0x40;
    private const int NID_READY = 0x80;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    /// <summary>
    /// タッチスクリーンが利用可能かを検出
    /// </summary>
    public bool IsTouchScreenAvailable()
    {
        try
        {
            int digitizer = GetSystemMetrics(SM_DIGITIZER);

            // タッチスクリーンが利用可能かチェック
            bool hasTouch = (digitizer & NID_INTEGRATED_TOUCH) != 0 ||
                           (digitizer & NID_EXTERNAL_TOUCH) != 0;
            bool isReady = (digitizer & NID_READY) != 0;

            return hasTouch && isReady;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 最大同時タッチポイント数を取得
    /// </summary>
    public int GetMaxTouchPoints()
    {
        try
        {
            return GetSystemMetrics(SM_MAXIMUMTOUCHES);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// マルチタッチ対応かを確認
    /// </summary>
    public bool IsMultiTouchSupported()
    {
        try
        {
            int digitizer = GetSystemMetrics(SM_DIGITIZER);
            return (digitizer & NID_MULTI_INPUT) != 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// タッチデバイス名を取得（WMI使用）
    /// </summary>
    public string? GetTouchDeviceName()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%Touch%' OR Name LIKE '%タッチ%'");

            foreach (ManagementObject obj in searcher.Get())
            {
                string? name = obj["Name"]?.ToString();
                string? deviceId = obj["DeviceID"]?.ToString();

                // HID準拠タッチスクリーンまたはタッチパネルデバイスを検出
                if (name != null && (
                    name.Contains("Touch Screen", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Touch Panel", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("タッチスクリーン") ||
                    name.Contains("タッチパネル")))
                {
                    return name;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// タップ検出を設定
    /// </summary>
    public void SetTapDetected(bool detected)
    {
        _tapDetected = detected;
    }

    /// <summary>
    /// スワイプ検出を設定
    /// </summary>
    public void SetSwipeDetected(bool detected)
    {
        _swipeDetected = detected;
    }

    /// <summary>
    /// 検査結果を生成
    /// </summary>
    public Task<TouchScreenResult> ExecuteAsync(string attachmentPath)
    {
        var result = new TouchScreenResult();

        try
        {
            // タッチスクリーンの検出
            result.TouchAvailable = IsTouchScreenAvailable();
            result.MaxTouchPoints = GetMaxTouchPoints();
            result.MultiTouchSupported = IsMultiTouchSupported();
            result.DeviceName = GetTouchDeviceName();

            if (!result.TouchAvailable)
            {
                result.Result = "not_available";
                result.Note = "タッチスクリーンが検出されませんでした";
                return Task.FromResult(result);
            }

            // テスト結果を設定
            result.TapDetected = _tapDetected;
            result.SwipeDetected = _swipeDetected;

            // 結果判定
            if (_tapDetected && _swipeDetected)
            {
                result.Result = "pass";
                result.Note = "タッチスクリーンは正常に動作しています";
            }
            else if (_tapDetected || _swipeDetected)
            {
                result.Result = "warn";
                result.Note = "一部の操作が検出されませんでした";
            }
            else
            {
                result.Result = "fail";
                result.Note = "タッチ操作が検出されませんでした（テストが実施されなかった可能性があります）";
            }
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            result.Result = "fail";
        }

        return Task.FromResult(result);
    }
}
