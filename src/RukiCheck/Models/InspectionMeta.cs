using System.Text.Json.Serialization;

namespace RukiCheck.Models;

/// <summary>
/// 検品メタ情報
/// </summary>
public class InspectionMeta
{
    [JsonPropertyName("management_id")]
    public string ManagementId { get; set; } = string.Empty;

    [JsonPropertyName("inspection_date")]
    public string InspectionDate { get; set; } = string.Empty;

    [JsonPropertyName("inspection_time")]
    public string InspectionTime { get; set; } = string.Empty;

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "standard";

    [JsonPropertyName("tool_version")]
    public string ToolVersion { get; set; } = "v0.1.0";
}
