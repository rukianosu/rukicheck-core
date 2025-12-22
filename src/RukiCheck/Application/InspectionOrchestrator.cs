using RukiCheck.Infrastructure;
using RukiCheck.Models;

namespace RukiCheck.Application;

/// <summary>
/// 検品プロセス全体を統括するオーケストレーター
/// </summary>
public class InspectionOrchestrator
{
    private readonly FileSystemService _fileSystem;
    private readonly JsonReportWriter _jsonWriter;
    private readonly HtmlReportGenerator _htmlGenerator;

    public InspectionOrchestrator(
        FileSystemService fileSystem,
        JsonReportWriter jsonWriter,
        HtmlReportGenerator htmlGenerator)
    {
        _fileSystem = fileSystem;
        _jsonWriter = jsonWriter;
        _htmlGenerator = htmlGenerator;
    }

    /// <summary>
    /// 新しい検品セッションを作成
    /// </summary>
    public InspectionSession CreateSession(string managementId, string basePath)
    {
        var now = DateTime.Now;
        var outputPath = _fileSystem.CreateInspectionFolder(basePath, managementId, now);

        return new InspectionSession
        {
            ManagementId = managementId,
            OutputPath = outputPath,
            Report = new InspectionReport
            {
                Meta = new InspectionMeta
                {
                    ManagementId = managementId,
                    InspectionDate = now.ToString("yyyy-MM-dd"),
                    InspectionTime = now.ToString("HH:mm:ss"),
                    Mode = "standard",
                    ToolVersion = "v0.1.0"
                }
            }
        };
    }

    /// <summary>
    /// レポートを保存（JSON + HTML）
    /// </summary>
    public async Task SaveReportAsync(InspectionSession session)
    {
        // JSON保存
        var jsonPath = Path.Combine(session.OutputPath, "report.json");
        await _jsonWriter.WriteAsync(jsonPath, session.Report);

        // HTML生成
        var htmlPath = Path.Combine(session.OutputPath, "report.html");
        await _htmlGenerator.GenerateAsync(htmlPath, session.Report);
    }

    /// <summary>
    /// 指定パスに書き込み権限があるか確認
    /// </summary>
    public bool CanWriteToPath(string path)
    {
        return _fileSystem.HasWritePermission(path);
    }
}
