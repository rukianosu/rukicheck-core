using System.IO;
using OpenCvSharp;
using RukiCheck.Models;

namespace RukiCheck.Services;

/// <summary>
/// カメラ検査サービス
/// </summary>
public class CameraInspectionService : IInspectionService<CameraResult>
{
    private VideoCapture? _capture;
    private string? _tempImagePath;

    /// <summary>
    /// カメラを開く（プレビュー用）
    /// </summary>
    public bool OpenCamera()
    {
        try
        {
            _capture = new VideoCapture(0); // 既定カメラ
            return _capture.IsOpened();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// フレームを取得（WPF表示用にbyte[]で返す）
    /// </summary>
    public byte[]? GrabFrame()
    {
        if (_capture == null || !_capture.IsOpened())
            return null;

        using var frame = new Mat();
        _capture.Read(frame);

        if (frame.Empty())
            return null;

        return frame.ToBytes(".jpg");
    }

    /// <summary>
    /// 静止画をキャプチャして一時保存
    /// </summary>
    public string? CaptureImage()
    {
        if (_capture == null || !_capture.IsOpened())
            return null;

        using var frame = new Mat();
        _capture.Read(frame);

        if (frame.Empty())
            return null;

        _tempImagePath = Path.Combine(Path.GetTempPath(), $"rukicheck_cam_{Guid.NewGuid()}.jpg");
        frame.SaveImage(_tempImagePath);

        return _tempImagePath;
    }

    /// <summary>
    /// カメラを閉じる
    /// </summary>
    public void CloseCamera()
    {
        _capture?.Release();
        _capture?.Dispose();
    }

    /// <summary>
    /// 検査を実行（最終的な保存処理）
    /// </summary>
    public Task<CameraResult> ExecuteAsync(string attachmentPath)
    {
        var result = new CameraResult();

        try
        {
            if (string.IsNullOrEmpty(_tempImagePath) || !File.Exists(_tempImagePath))
            {
                result.Captured = false;
                result.Error = "キャプチャ画像が見つかりません";
                return Task.FromResult(result);
            }

            // attachmentsフォルダへ移動
            var fileName = "camera_test.jpg";
            var destPath = Path.Combine(attachmentPath, fileName);

            File.Copy(_tempImagePath, destPath, overwrite: true);

            // TEMP削除
            try { File.Delete(_tempImagePath); } catch { /* ignore */ }

            result.Captured = true;
            result.File = $"attachments/{fileName}";
            result.DeviceName = "既定カメラ";
        }
        catch (UnauthorizedAccessException)
        {
            result.Captured = false;
            result.Error = "カメラアクセス権限エラー";
            result.Note = "カメラはWindowsプライバシー設定により取得不可";
        }
        catch (Exception ex)
        {
            result.Captured = false;
            result.Error = $"キャプチャ失敗: {ex.Message}";
        }

        return Task.FromResult(result);
    }
}
