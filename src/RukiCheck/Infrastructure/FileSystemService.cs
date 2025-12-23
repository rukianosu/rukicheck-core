using System.IO;

namespace RukiCheck.Infrastructure;

/// <summary>
/// ファイルシステム操作サービス
/// </summary>
public class FileSystemService
{
    /// <summary>
    /// 検品用フォルダを作成
    /// </summary>
    /// <param name="basePath">ベースパス（例: E:\）</param>
    /// <param name="managementId">管理番号</param>
    /// <param name="date">検品日</param>
    /// <returns>作成されたフォルダの絶対パス</returns>
    public string CreateInspectionFolder(string basePath, string managementId, DateTime date)
    {
        // {USB}:\RukiCheck\{管理番号}_{YYYYMMDD}\
        var folderName = $"{managementId}_{date:yyyyMMdd}";
        var rootPath = Path.Combine(basePath, "RukiCheck");
        var fullPath = Path.Combine(rootPath, folderName);

        // ルートフォルダ作成
        if (!Directory.Exists(rootPath))
        {
            Directory.CreateDirectory(rootPath);
        }

        // 検品フォルダ作成
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        // attachmentsサブフォルダ作成
        var attachmentsPath = Path.Combine(fullPath, "attachments");
        if (!Directory.Exists(attachmentsPath))
        {
            Directory.CreateDirectory(attachmentsPath);
        }

        return fullPath;
    }

    /// <summary>
    /// 指定パスに書き込み権限があるかテスト
    /// </summary>
    public bool HasWritePermission(string path)
    {
        try
        {
            var testFile = Path.Combine(path, $".test_{Guid.NewGuid()}.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// attachmentsフォルダのパスを取得
    /// </summary>
    public string GetAttachmentsPath(string inspectionFolderPath)
    {
        return Path.Combine(inspectionFolderPath, "attachments");
    }
}
