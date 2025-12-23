using System.Text.Json.Serialization;

namespace RukiCheck.Models;

/// <summary>
/// CD/DVDドライブ検査結果
/// </summary>
public class CdDvdResult
{
    /// <summary>
    /// 光学ドライブが見つかったか
    /// </summary>
    [JsonPropertyName("drive_found")]
    public bool DriveFound { get; set; }

    /// <summary>
    /// ドライブレター（例: D:）
    /// </summary>
    [JsonPropertyName("drive_letter")]
    public string DriveLetter { get; set; } = string.Empty;

    /// <summary>
    /// ドライブ名（例: DVD-RWドライブ）
    /// </summary>
    [JsonPropertyName("drive_name")]
    public string DriveName { get; set; } = string.Empty;

    /// <summary>
    /// メディアが挿入されているか
    /// </summary>
    [JsonPropertyName("media_inserted")]
    public bool MediaInserted { get; set; }

    /// <summary>
    /// メディアのラベル名
    /// </summary>
    [JsonPropertyName("media_label")]
    public string? MediaLabel { get; set; }

    /// <summary>
    /// メディアの総容量（GB）
    /// </summary>
    [JsonPropertyName("media_total_gb")]
    public double? MediaTotalGb { get; set; }

    /// <summary>
    /// メディアの空き容量（GB）
    /// </summary>
    [JsonPropertyName("media_free_gb")]
    public double? MediaFreeGb { get; set; }

    /// <summary>
    /// 読み込みテストが成功したか
    /// </summary>
    [JsonPropertyName("read_test_success")]
    public bool ReadTestSuccess { get; set; }

    /// <summary>
    /// 読み込んだファイル数
    /// </summary>
    [JsonPropertyName("files_read")]
    public int FilesRead { get; set; }

    /// <summary>
    /// ユーザー確認（読み込みできたか）
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
