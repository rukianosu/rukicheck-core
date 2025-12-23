using System.Management;
using RukiCheck.Models;

namespace RukiCheck.Services;

/// <summary>
/// Bluetooth検査サービス（デバイススキャンと検出確認）
/// </summary>
public class BluetoothInspectionService : IInspectionService<BluetoothResult>
{
    private bool _userConfirmed = false;
    private List<BluetoothDevice> _discoveredDevices = new();
    private List<BluetoothDevice> _pairedDevices = new();

    /// <summary>
    /// Bluetoothデバイススキャンを実行
    /// </summary>
    public async Task<bool> ScanDevicesAsync()
    {
        try
        {
            _discoveredDevices.Clear();
            _pairedDevices.Clear();

            // WMI経由でペアリング済みBluetoothデバイスを取得
            // 注: 実際のデバイススキャンにはWindows.Devices.Bluetooth APIが必要
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE Service='BTHUSB' OR Service='BthEnum' OR Name LIKE '%Bluetooth%'");

            foreach (ManagementObject device in searcher.Get())
            {
                var name = device["Name"]?.ToString();
                var deviceId = device["DeviceID"]?.ToString();

                if (!string.IsNullOrEmpty(name) && !name.Contains("Adapter") && !name.Contains("Enumerator"))
                {
                    var btDevice = new BluetoothDevice
                    {
                        Name = name,
                        Address = ExtractAddressFromDeviceId(deviceId),
                        IsPaired = true,
                        IsConnected = false,
                        DeviceType = DetermineDeviceType(name)
                    };

                    _pairedDevices.Add(btDevice);
                }
            }

            await Task.Delay(2000); // スキャンのシミュレーション

            // ペアリング済みデバイスを検出デバイスにも追加
            _discoveredDevices.AddRange(_pairedDevices);

            return _discoveredDevices.Count > 0 || _pairedDevices.Count > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Bluetoothスキャンエラー: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// ユーザー確認結果を設定
    /// </summary>
    public void SetUserConfirmation(bool confirmed)
    {
        _userConfirmed = confirmed;
    }

    /// <summary>
    /// 検査結果を生成
    /// </summary>
    public Task<BluetoothResult> ExecuteAsync(string attachmentPath)
    {
        var result = new BluetoothResult
        {
            UserConfirmed = _userConfirmed,
            DiscoveredDevices = _discoveredDevices,
            PairedDevices = _pairedDevices,
            DevicesFound = _discoveredDevices.Count,
            ScanSuccessful = _discoveredDevices.Count > 0 || _pairedDevices.Count > 0
        };

        try
        {
            // Bluetoothアダプター情報を取得
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%Bluetooth%' AND (Service='BTHUSB' OR Service='BthLEEnum' OR Name LIKE '%Adapter%')");

            foreach (ManagementObject adapter in searcher.Get())
            {
                var name = adapter["Name"]?.ToString();
                if (!string.IsNullOrEmpty(name) && name.Contains("Adapter"))
                {
                    result.AdapterFound = true;
                    result.AdapterName = name;

                    // アダプターの状態を取得
                    var status = adapter["Status"]?.ToString();
                    var configManagerErrorCode = adapter["ConfigManagerErrorCode"];

                    if (configManagerErrorCode != null && Convert.ToInt32(configManagerErrorCode) == 0)
                    {
                        result.IsEnabled = true;
                    }

                    break; // 最初のアダプターのみ使用
                }
            }

            if (!result.AdapterFound)
            {
                result.Error = "Bluetoothアダプターが見つかりませんでした";
                result.AdapterName = "検出されませんでした";
                result.IsEnabled = false;
            }
        }
        catch (Exception ex)
        {
            result.Error = $"Bluetooth情報取得エラー: {ex.Message}";
            result.AdapterName = "取得失敗";
            result.IsEnabled = false;
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// デバイスIDからBluetoothアドレスを抽出（簡易版）
    /// </summary>
    private string ExtractAddressFromDeviceId(string? deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
            return "不明";

        // デバイスIDからアドレスらしき部分を抽出（完全ではない）
        var parts = deviceId.Split('\\', '&', '_');
        foreach (var part in parts)
        {
            if (part.Length == 12 && part.All(c => char.IsLetterOrDigit(c)))
            {
                // 12桁の16進数をMACアドレス形式に変換
                return string.Join(":", Enumerable.Range(0, 6)
                    .Select(i => part.Substring(i * 2, 2)));
            }
        }

        return "不明";
    }

    /// <summary>
    /// デバイス名からデバイスタイプを推定
    /// </summary>
    private string DetermineDeviceType(string name)
    {
        var lowerName = name.ToLower();

        if (lowerName.Contains("mouse")) return "マウス";
        if (lowerName.Contains("keyboard")) return "キーボード";
        if (lowerName.Contains("headphone") || lowerName.Contains("headset")) return "ヘッドフォン";
        if (lowerName.Contains("speaker")) return "スピーカー";
        if (lowerName.Contains("phone")) return "スマートフォン";
        if (lowerName.Contains("controller")) return "ゲームコントローラー";

        return "不明";
    }
}
