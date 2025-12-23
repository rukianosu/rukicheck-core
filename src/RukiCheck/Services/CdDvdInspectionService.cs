using System.IO;
using System.Management;
using RukiCheck.Models;

namespace RukiCheck.Services;

/// <summary>
/// CD/DVDドライブ検査サービス（ドライブ検出と読み込みテスト）
/// </summary>
public class CdDvdInspectionService : IInspectionService<CdDvdResult>
{
    private bool _userConfirmed = false;
    private DriveInfo? _cdDrive = null;

    /// <summary>
    /// 光学ドライブをスキャン
    /// </summary>
    public bool ScanOpticalDrive()
    {
        try
        {
            // すべてのドライブから光学ドライブを検索
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                if (drive.DriveType == DriveType.CDRom)
                {
                    _cdDrive = drive;
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"光学ドライブスキャンエラー: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// メディアが挿入されているか確認
    /// </summary>
    public bool IsMediaInserted()
    {
        if (_cdDrive == null)
            return false;

        try
        {
            return _cdDrive.IsReady;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// メディアからファイルを読み込むテスト
    /// </summary>
    public async Task<(bool success, int fileCount)> TestReadAsync()
    {
        if (_cdDrive == null || !_cdDrive.IsReady)
            return (false, 0);

        try
        {
            // ルートディレクトリのファイルとフォルダを列挙して読み込みテスト
            var files = Directory.GetFiles(_cdDrive.Name, "*", SearchOption.TopDirectoryOnly);
            var directories = Directory.GetDirectories(_cdDrive.Name, "*", SearchOption.TopDirectoryOnly);

            await Task.Delay(500); // 読み込みシミュレーション

            return (true, files.Length + directories.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CD/DVD読み込みエラー: {ex.Message}");
            return (false, 0);
        }
    }

    /// <summary>
    /// ユーザー確認結果を設定
    /// </summary>
    public void SetUserConfirmation(bool confirmed)
    {
        _userConfirmed = confirmed;
    }

    /// <summary>
    /// 検査結果を生成
    /// </summary>
    public Task<CdDvdResult> ExecuteAsync(string attachmentPath)
    {
        var result = new CdDvdResult
        {
            UserConfirmed = _userConfirmed
        };

        try
        {
            // WMI経由で光学ドライブ情報を取得
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_CDROMDrive");

            foreach (ManagementObject drive in searcher.Get())
            {
                result.DriveFound = true;
                result.DriveName = drive["Name"]?.ToString() ?? "不明";
                result.DriveLetter = drive["Drive"]?.ToString() ?? "不明";

                break; // 最初のドライブのみ使用
            }

            if (!result.DriveFound)
            {
                result.Error = "光学ドライブが見つかりませんでした";
                result.DriveName = "検出されませんでした";
                result.Note = "CD/DVDドライブが搭載されていない可能性があります";
                return Task.FromResult(result);
            }

            // DriveInfoから詳細情報を取得
            if (_cdDrive != null)
            {
                result.DriveLetter = _cdDrive.Name;
                result.MediaInserted = _cdDrive.IsReady;

                if (_cdDrive.IsReady)
                {
                    try
                    {
                        result.MediaLabel = _cdDrive.VolumeLabel;
                        result.MediaTotalGb = _cdDrive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                        result.MediaFreeGb = _cdDrive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);

                        // 読み込みテスト結果を反映
                        var (success, fileCount) = TestReadAsync().Result;
                        result.ReadTestSuccess = success;
                        result.FilesRead = fileCount;
                    }
                    catch (Exception ex)
                    {
                        result.Error = $"メディア情報取得エラー: {ex.Message}";
                        result.MediaInserted = false;
                    }
                }
                else
                {
                    result.Note = "メディアが挿入されていません。テスト用のCD/DVDを挿入してください。";
                }
            }
        }
        catch (Exception ex)
        {
            result.Error = $"CD/DVDドライブ情報取得エラー: {ex.Message}";
            result.DriveFound = false;
        }

        return Task.FromResult(result);
    }
}
