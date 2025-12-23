using System.Text.Json.Serialization;

namespace RukiCheck.Models;

/// <summary>
/// メモリテスト結果
/// </summary>
public class MemoryTestResult
{
    /// <summary>
    /// テストが実行されたか
    /// </summary>
    [JsonPropertyName("test_executed")]
    public bool TestExecuted { get; set; }

    /// <summary>
    /// テストモード（Standard / Thorough）
    /// </summary>
    [JsonPropertyName("test_mode")]
    public string? TestMode { get; set; }

    /// <summary>
    /// テストしたメモリサイズ（MB）
    /// </summary>
    [JsonPropertyName("tested_mb")]
    public int TestedMb { get; set; }

    /// <summary>
    /// 実行したパス数
    /// </summary>
    [JsonPropertyName("total_passes")]
    public int TotalPasses { get; set; }

    /// <summary>
    /// 利用可能なメモリ（MB）
    /// </summary>
    [JsonPropertyName("available_mb")]
    public long AvailableMb { get; set; }

    /// <summary>
    /// 総物理メモリ（GB）
    /// </summary>
    [JsonPropertyName("total_gb")]
    public double TotalGb { get; set; }

    /// <summary>
    /// テスト実行時間（秒）
    /// </summary>
    [JsonPropertyName("test_duration_sec")]
    public double TestDurationSec { get; set; }

    /// <summary>
    /// テストパターン数
    /// </summary>
    [JsonPropertyName("patterns_tested")]
    public int PatternsTested { get; set; }

    /// <summary>
    /// 全てのパターンテストが成功したか
    /// </summary>
    [JsonPropertyName("all_patterns_passed")]
    public bool AllPatternsPassed { get; set; }

    /// <summary>
    /// 失敗したパターン数
    /// </summary>
    [JsonPropertyName("failed_patterns")]
    public int FailedPatterns { get; set; }

    /// <summary>
    /// 書き込み速度（MB/s）
    /// </summary>
    [JsonPropertyName("write_speed_mbps")]
    public double WriteSpeedMbps { get; set; }

    /// <summary>
    /// 読み込み速度（MB/s）
    /// </summary>
    [JsonPropertyName("read_speed_mbps")]
    public double ReadSpeedMbps { get; set; }

    /// <summary>
    /// ユーザー確認（異常なフリーズなどがなかったか）
    /// </summary>
    [JsonPropertyName("user_confirmed")]
    public bool UserConfirmed { get; set; }

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
