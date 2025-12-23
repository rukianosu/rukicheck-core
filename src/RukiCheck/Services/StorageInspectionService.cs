using System.IO;
using RukiCheck.Models;

namespace RukiCheck.Services;

/// <summary>
/// ストレージ（Cドライブ）検査サービス
/// </summary>
public class StorageInspectionService : IInspectionService<StorageResult>
{
    public Task<StorageResult> ExecuteAsync(string attachmentPath)
    {
        var result = new StorageResult();

        try
        {
            var cDrive = DriveInfo.GetDrives().FirstOrDefault(d => d.Name == "C:\\");

            if (cDrive == null || !cDrive.IsReady)
            {
                result.Error = "Cドライブが見つからないか、アクセスできません";
                result.Status = "error";
                return Task.FromResult(result);
            }

            // 容量計算（GB単位）
            result.Drive = "C:";
            result.TotalGb = Math.Round(cDrive.TotalSize / (1024.0 * 1024.0 * 1024.0), 1);
            result.FreeGb = Math.Round(cDrive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0), 1);
            result.FreePercent = Math.Round((result.FreeGb / result.TotalGb) * 100, 1);

            // 判定ロジック
            if (result.FreePercent >= 20)
            {
                result.Status = "ok";
                result.Recommendation = null;
            }
            else if (result.FreePercent >= 10 && result.FreePercent < 20)
            {
                result.Status = "warn";
                result.Recommendation = "空き容量が少なくなっています。不要ファイルの削除を推奨します。";
            }
            else if (result.FreePercent < 10 || result.FreeGb < 20)
            {
                result.Status = "danger";
                result.Recommendation = "SSD容量アップ推奨（例：512GB → 1TB）または大幅なファイル整理が必要です。";
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            result.Error = $"権限エラー: {ex.Message}";
            result.Status = "error";
        }
        catch (Exception ex)
        {
            result.Error = $"検査失敗: {ex.Message}";
            result.Status = "error";
        }

        return Task.FromResult(result);
    }
}
