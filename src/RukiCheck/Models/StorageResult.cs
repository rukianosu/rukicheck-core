using System.Text.Json.Serialization;

namespace RukiCheck.Models;

/// <summary>
/// ストレージ検査結果
/// </summary>
public class StorageResult
{
    [JsonPropertyName("drive")]
    public string Drive { get; set; } = string.Empty;

    [JsonPropertyName("total_gb")]
    public double TotalGb { get; set; }

    [JsonPropertyName("free_gb")]
    public double FreeGb { get; set; }

    [JsonPropertyName("free_percent")]
    public double FreePercent { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "unknown"; // ok / warn / danger

    [JsonPropertyName("recommendation")]
    public string? Recommendation { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
