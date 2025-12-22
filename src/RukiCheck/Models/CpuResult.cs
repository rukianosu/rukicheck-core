using System.Text.Json.Serialization;

namespace RukiCheck.Models;

/// <summary>
/// CPU検査結果
/// </summary>
public class CpuResult
{
    [JsonPropertyName("stress_test")]
    public string StressTest { get; set; } = "not_started"; // completed / failed / not_started

    [JsonPropertyName("duration_sec")]
    public int DurationSec { get; set; }

    [JsonPropertyName("abnormal")]
    public bool Abnormal { get; set; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
