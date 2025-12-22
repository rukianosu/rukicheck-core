using System.Text.Json.Serialization;

namespace RukiCheck.Models;

/// <summary>
/// ハードウェア情報収集結果
/// </summary>
public class HardwareInfoResult
{
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
