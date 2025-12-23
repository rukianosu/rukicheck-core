using System.IO;
using RukiCheck.Models;

namespace RukiCheck.Application;

/// <summary>
/// 検品セッション情報
/// </summary>
public class InspectionSession
{
    /// <summary>
    /// 管理番号
    /// </summary>
    public string ManagementId { get; set; } = string.Empty;

    /// <summary>
    /// 出力フォルダパス
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// 検品レポートデータ
    /// </summary>
    public InspectionReport Report { get; set; } = new();

    /// <summary>
    /// attachmentsフォルダのパス
    /// </summary>
    public string AttachmentsPath => Path.Combine(OutputPath, "attachments");
}
