using System.Text.Json.Serialization;

namespace RukiCheck.Models;

/// <summary>
/// Bluetooth検査結果
/// </summary>
public class BluetoothResult
{
    /// <summary>
    /// Bluetoothアダプターが検出されたか
    /// </summary>
    [JsonPropertyName("adapter_found")]
    public bool AdapterFound { get; set; }

    /// <summary>
    /// アダプター名
    /// </summary>
    [JsonPropertyName("adapter_name")]
    public string AdapterName { get; set; } = string.Empty;

    /// <summary>
    /// Bluetoothが有効かどうか
    /// </summary>
    [JsonPropertyName("is_enabled")]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// スキャンが成功したか
    /// </summary>
    [JsonPropertyName("scan_successful")]
    public bool ScanSuccessful { get; set; }

    /// <summary>
    /// 検出されたデバイス数
    /// </summary>
    [JsonPropertyName("devices_found")]
    public int DevicesFound { get; set; }

    /// <summary>
    /// 検出されたデバイスのリスト
    /// </summary>
    [JsonPropertyName("discovered_devices")]
    public List<BluetoothDevice> DiscoveredDevices { get; set; } = new();

    /// <summary>
    /// ペアリング済みデバイスのリスト
    /// </summary>
    [JsonPropertyName("paired_devices")]
    public List<BluetoothDevice> PairedDevices { get; set; } = new();

    /// <summary>
    /// ユーザーがデバイス検出を確認したか
    /// </summary>
    [JsonPropertyName("user_confirmed")]
    public bool UserConfirmed { get; set; }

    /// <summary>
    /// エラーメッセージ
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// Bluetoothデバイス情報
/// </summary>
public class BluetoothDevice
{
    /// <summary>
    /// デバイス名
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// デバイスアドレス（MACアドレス）
    /// </summary>
    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// ペアリング済みかどうか
    /// </summary>
    [JsonPropertyName("is_paired")]
    public bool IsPaired { get; set; }

    /// <summary>
    /// 接続中かどうか
    /// </summary>
    [JsonPropertyName("is_connected")]
    public bool IsConnected { get; set; }

    /// <summary>
    /// デバイスタイプ（ヘッドフォン、マウス、キーボードなど）
    /// </summary>
    [JsonPropertyName("device_type")]
    public string DeviceType { get; set; } = string.Empty;

    /// <summary>
    /// 信号強度（RSSI）
    /// </summary>
    [JsonPropertyName("signal_strength")]
    public int? SignalStrength { get; set; }
}
