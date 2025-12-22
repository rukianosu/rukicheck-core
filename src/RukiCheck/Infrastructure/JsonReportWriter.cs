using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using RukiCheck.Models;

namespace RukiCheck.Infrastructure;

/// <summary>
/// JSON レポート出力サービス
/// </summary>
public class JsonReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), // 日本語をエスケープしない
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    /// <summary>
    /// レポートをJSON形式で保存
    /// </summary>
    public async Task WriteAsync(string filePath, InspectionReport report)
    {
        var json = JsonSerializer.Serialize(report, JsonOptions);

        // UTF-8 BOM付きで保存（Windows互換性）
        var utf8WithBom = new UTF8Encoding(true);
        await File.WriteAllTextAsync(filePath, json, utf8WithBom);
    }
}
