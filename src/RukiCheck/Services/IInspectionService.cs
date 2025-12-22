namespace RukiCheck.Services;

/// <summary>
/// 検査サービスの共通インターフェース
/// </summary>
/// <typeparam name="T">検査結果の型</typeparam>
public interface IInspectionService<T>
{
    /// <summary>
    /// 検査を実行する
    /// </summary>
    /// <param name="attachmentPath">証拠ファイル保存先パス（audio/image用）</param>
    /// <returns>検査結果</returns>
    Task<T> ExecuteAsync(string attachmentPath);
}
