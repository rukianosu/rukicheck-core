using System.Text.Json.Serialization;

namespace RukiCheck.Models;

/// <summary>
/// ハードウェア情報収集結果
/// </summary>
public class HardwareInfoResult
{
    /// <summary>
    /// システム情報（メーカー、型番、シリアルナンバー）
    /// </summary>
    [JsonPropertyName("system")]
    public SystemInfo System { get; set; } = new();

    /// <summary>
    /// メモリ情報
    /// </summary>
    [JsonPropertyName("memory")]
    public MemoryInfo Memory { get; set; } = new();

    /// <summary>
    /// CPU情報
    /// </summary>
    [JsonPropertyName("cpu")]
    public CpuInfo Cpu { get; set; } = new();

    /// <summary>
    /// GPU情報（複数のGPUがある場合はリスト）
    /// </summary>
    [JsonPropertyName("gpu")]
    public List<GpuInfo> Gpus { get; set; } = new();

    /// <summary>
    /// BitLocker情報
    /// </summary>
    [JsonPropertyName("bitlocker")]
    public BitLockerInfo BitLocker { get; set; } = new();

    /// <summary>
    /// ストレージヘルス情報（SMART/NVMe）
    /// </summary>
    [JsonPropertyName("storage_health")]
    public List<StorageHealthInfo> StorageHealth { get; set; } = new();

    /// <summary>
    /// ウイルス対策情報（Windows Defender、USBスキャン結果）
    /// </summary>
    [JsonPropertyName("antivirus")]
    public AntivirusInfo Antivirus { get; set; } = new();
}

/// <summary>
/// システム情報
/// </summary>
public class SystemInfo
{
    /// <summary>
    /// メーカー名（例: Dell Inc., HP, Lenovo）
    /// </summary>
    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// 型番・モデル名（例: Latitude 7490, ThinkPad X1 Carbon）
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// シリアルナンバー
    /// </summary>
    [JsonPropertyName("serial_number")]
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// BIOSバージョン
    /// </summary>
    [JsonPropertyName("bios_version")]
    public string BiosVersion { get; set; } = string.Empty;
}

/// <summary>
/// メモリ情報
/// </summary>
public class MemoryInfo
{
    /// <summary>
    /// 総容量（GB）
    /// </summary>
    [JsonPropertyName("total_gb")]
    public double TotalGb { get; set; }

    /// <summary>
    /// メモリ種別（DDR2, DDR3, DDR4, DDR5など）
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 速度（MHz）
    /// </summary>
    [JsonPropertyName("speed_mhz")]
    public int SpeedMhz { get; set; }

    /// <summary>
    /// スロット数
    /// </summary>
    [JsonPropertyName("slots")]
    public int Slots { get; set; }

    /// <summary>
    /// メモリモジュール詳細リスト
    /// </summary>
    [JsonPropertyName("modules")]
    public List<MemoryModule> Modules { get; set; } = new();
}

/// <summary>
/// 個別メモリモジュール情報
/// </summary>
public class MemoryModule
{
    /// <summary>
    /// 容量（GB）
    /// </summary>
    [JsonPropertyName("capacity_gb")]
    public double CapacityGb { get; set; }

    /// <summary>
    /// メーカー
    /// </summary>
    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// 速度（MHz）
    /// </summary>
    [JsonPropertyName("speed_mhz")]
    public int SpeedMhz { get; set; }

    /// <summary>
    /// パーツ番号
    /// </summary>
    [JsonPropertyName("part_number")]
    public string PartNumber { get; set; } = string.Empty;
}

/// <summary>
/// CPU情報
/// </summary>
public class CpuInfo
{
    /// <summary>
    /// CPU名（モデル名）
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 製造元
    /// </summary>
    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// 物理コア数
    /// </summary>
    [JsonPropertyName("cores")]
    public int Cores { get; set; }

    /// <summary>
    /// 論理プロセッサ数（スレッド数）
    /// </summary>
    [JsonPropertyName("logical_processors")]
    public int LogicalProcessors { get; set; }

    /// <summary>
    /// 最大クロック速度（MHz）
    /// </summary>
    [JsonPropertyName("max_clock_mhz")]
    public int MaxClockMhz { get; set; }

    /// <summary>
    /// アーキテクチャ（x64, ARM64など）
    /// </summary>
    [JsonPropertyName("architecture")]
    public string Architecture { get; set; } = string.Empty;
}

/// <summary>
/// GPU情報
/// </summary>
public class GpuInfo
{
    /// <summary>
    /// GPU名
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// VRAM容量（GB）
    /// </summary>
    [JsonPropertyName("vram_gb")]
    public double VramGb { get; set; }

    /// <summary>
    /// ドライバーバージョン
    /// </summary>
    [JsonPropertyName("driver_version")]
    public string DriverVersion { get; set; } = string.Empty;

    /// <summary>
    /// 製造元（NVIDIA, AMD, Intelなど）
    /// </summary>
    [JsonPropertyName("adapter_manufacturer")]
    public string AdapterManufacturer { get; set; } = string.Empty;

    /// <summary>
    /// ビデオプロセッサ
    /// </summary>
    [JsonPropertyName("video_processor")]
    public string VideoProcessor { get; set; } = string.Empty;
}

/// <summary>
/// BitLocker情報
/// </summary>
public class BitLockerInfo
{
    /// <summary>
    /// BitLockerボリュームリスト
    /// </summary>
    [JsonPropertyName("volumes")]
    public List<BitLockerVolume> Volumes { get; set; } = new();

    /// <summary>
    /// 取得エラー（権限不足など）
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// 注記（管理者権限が必要な旨など）
    /// </summary>
    [JsonPropertyName("note")]
    public string? Note { get; set; }
}

/// <summary>
/// BitLockerボリューム情報
/// </summary>
public class BitLockerVolume
{
    /// <summary>
    /// ドライブレター（例: C:）
    /// </summary>
    [JsonPropertyName("drive_letter")]
    public string DriveLetter { get; set; } = string.Empty;

    /// <summary>
    /// 暗号化されているか
    /// </summary>
    [JsonPropertyName("is_encrypted")]
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// 保護状態（Protection On/Off）
    /// </summary>
    [JsonPropertyName("protection_status")]
    public string ProtectionStatus { get; set; } = string.Empty;

    /// <summary>
    /// 暗号化の割合（%）
    /// </summary>
    [JsonPropertyName("encryption_percentage")]
    public int EncryptionPercentage { get; set; }

    /// <summary>
    /// 回復キー（取得できた場合）
    /// </summary>
    [JsonPropertyName("recovery_keys")]
    public List<string> RecoveryKeys { get; set; } = new();

    /// <summary>
    /// エラーメッセージ（取得失敗時）
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// ストレージヘルス情報（SMART/NVMe）
/// </summary>
public class StorageHealthInfo
{
    /// <summary>
    /// ドライブ番号（例: 0, 1, 2）
    /// </summary>
    [JsonPropertyName("drive_number")]
    public int DriveNumber { get; set; }

    /// <summary>
    /// モデル名
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// シリアル番号
    /// </summary>
    [JsonPropertyName("serial_number")]
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// インターフェースタイプ（SATA, NVMe, USB, など）
    /// </summary>
    [JsonPropertyName("interface_type")]
    public string InterfaceType { get; set; } = string.Empty;

    /// <summary>
    /// メディアタイプ（SSD, HDD, など）
    /// </summary>
    [JsonPropertyName("media_type")]
    public string MediaType { get; set; } = string.Empty;

    /// <summary>
    /// 容量（GB）
    /// </summary>
    [JsonPropertyName("capacity_gb")]
    public double CapacityGb { get; set; }

    /// <summary>
    /// 健康状態（OK, Warning, Critical, Unknown）
    /// </summary>
    [JsonPropertyName("health_status")]
    public string HealthStatus { get; set; } = "Unknown";

    /// <summary>
    /// 温度（℃）
    /// </summary>
    [JsonPropertyName("temperature_celsius")]
    public int? TemperatureCelsius { get; set; }

    /// <summary>
    /// 通電時間（時間）
    /// </summary>
    [JsonPropertyName("power_on_hours")]
    public long? PowerOnHours { get; set; }

    /// <summary>
    /// 残り寿命（%）SSDの場合
    /// </summary>
    [JsonPropertyName("remaining_life_percent")]
    public int? RemainingLifePercent { get; set; }

    /// <summary>
    /// 総書き込み量（GB）
    /// </summary>
    [JsonPropertyName("total_bytes_written_gb")]
    public double? TotalBytesWrittenGb { get; set; }

    /// <summary>
    /// 総読み込み量（GB）
    /// </summary>
    [JsonPropertyName("total_bytes_read_gb")]
    public double? TotalBytesReadGb { get; set; }

    /// <summary>
    /// Critical Warning（NVMeの場合）
    /// </summary>
    [JsonPropertyName("critical_warning")]
    public string? CriticalWarning { get; set; }

    /// <summary>
    /// SMART属性リスト
    /// </summary>
    [JsonPropertyName("smart_attributes")]
    public List<SmartAttribute> SmartAttributes { get; set; } = new();

    /// <summary>
    /// エラーメッセージ
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// 注記
    /// </summary>
    [JsonPropertyName("note")]
    public string? Note { get; set; }
}

/// <summary>
/// SMART属性
/// </summary>
public class SmartAttribute
{
    /// <summary>
    /// 属性ID
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// 属性名
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 現在値
    /// </summary>
    [JsonPropertyName("current_value")]
    public int CurrentValue { get; set; }

    /// <summary>
    /// 最悪値
    /// </summary>
    [JsonPropertyName("worst_value")]
    public int WorstValue { get; set; }

    /// <summary>
    /// しきい値
    /// </summary>
    [JsonPropertyName("threshold")]
    public int Threshold { get; set; }

    /// <summary>
    /// RAWデータ
    /// </summary>
    [JsonPropertyName("raw_value")]
    public long RawValue { get; set; }
}

/// <summary>
/// ウイルス対策情報
/// </summary>
public class AntivirusInfo
{
    /// <summary>
    /// Windows Defender情報
    /// </summary>
    [JsonPropertyName("windows_defender")]
    public WindowsDefenderInfo WindowsDefender { get; set; } = new();

    /// <summary>
    /// USBドライブスキャン結果
    /// </summary>
    [JsonPropertyName("usb_scan")]
    public UsbScanResult UsbScan { get; set; } = new();

    /// <summary>
    /// エラーメッセージ
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// 注記
    /// </summary>
    [JsonPropertyName("note")]
    public string? Note { get; set; }
}

/// <summary>
/// Windows Defender情報
/// </summary>
public class WindowsDefenderInfo
{
    /// <summary>
    /// Windows Defenderが有効か
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// リアルタイム保護が有効か
    /// </summary>
    [JsonPropertyName("realtime_protection_enabled")]
    public bool RealtimeProtectionEnabled { get; set; }

    /// <summary>
    /// 定義ファイルの最終更新日時
    /// </summary>
    [JsonPropertyName("signature_last_updated")]
    public string SignatureLastUpdated { get; set; } = string.Empty;

    /// <summary>
    /// 最終スキャン日時
    /// </summary>
    [JsonPropertyName("last_scan_datetime")]
    public string LastScanDateTime { get; set; } = string.Empty;

    /// <summary>
    /// 最近検出された脅威の数
    /// </summary>
    [JsonPropertyName("recent_threats_count")]
    public int RecentThreatsCount { get; set; }

    /// <summary>
    /// 定義ファイルが最新かどうか（7日以内の更新）
    /// </summary>
    [JsonPropertyName("signature_up_to_date")]
    public bool SignatureUpToDate { get; set; }

    /// <summary>
    /// エラーメッセージ
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// USBドライブスキャン結果
/// </summary>
public class UsbScanResult
{
    /// <summary>
    /// スキャンを実行したか
    /// </summary>
    [JsonPropertyName("scanned")]
    public bool Scanned { get; set; }

    /// <summary>
    /// スキャン対象パス
    /// </summary>
    [JsonPropertyName("scan_path")]
    public string ScanPath { get; set; } = string.Empty;

    /// <summary>
    /// スキャン結果（Clean/Threat/Error）
    /// </summary>
    [JsonPropertyName("result")]
    public string Result { get; set; } = "Unknown";

    /// <summary>
    /// 検出された脅威の数
    /// </summary>
    [JsonPropertyName("threats_found")]
    public int ThreatsFound { get; set; }

    /// <summary>
    /// 検出された脅威のリスト
    /// </summary>
    [JsonPropertyName("threat_names")]
    public List<string> ThreatNames { get; set; } = new();

    /// <summary>
    /// スキャンにかかった時間（秒）
    /// </summary>
    [JsonPropertyName("scan_duration_seconds")]
    public double ScanDurationSeconds { get; set; }

    /// <summary>
    /// エラーメッセージ
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// 警告メッセージ
    /// </summary>
    [JsonPropertyName("warning")]
    public string? Warning { get; set; }
}
