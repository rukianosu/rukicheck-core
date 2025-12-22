using NAudio.Wave;
using RukiCheck.Models;

namespace RukiCheck.Services;

/// <summary>
/// マイク検査サービス（録音・再生）
/// </summary>
public class MicrophoneInspectionService : IInspectionService<MicrophoneResult>
{
    private const int RecordingDurationSeconds = 5;
    private WaveInEvent? _waveIn;
    private WaveFileWriter? _writer;
    private string? _tempFilePath;
    private bool _userConfirmed = false;

    /// <summary>
    /// 録音を開始
    /// </summary>
    public async Task<string> StartRecordingAsync()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"rukicheck_mic_{Guid.NewGuid()}.wav");

        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(44100, 1) // 44.1kHz, モノラル
        };

        _writer = new WaveFileWriter(_tempFilePath, _waveIn.WaveFormat);

        _waveIn.DataAvailable += (sender, e) =>
        {
            _writer?.Write(e.Buffer, 0, e.BytesRecorded);
        };

        _waveIn.StartRecording();

        // 5秒間録音
        await Task.Delay(RecordingDurationSeconds * 1000);

        _waveIn.StopRecording();
        _waveIn.Dispose();
        _writer?.Dispose();

        return _tempFilePath;
    }

    /// <summary>
    /// 録音ファイルを再生
    /// </summary>
    public async Task PlayRecordingAsync(string filePath)
    {
        using var audioFile = new AudioFileReader(filePath);
        using var outputDevice = new WaveOutEvent();

        outputDevice.Init(audioFile);
        outputDevice.Play();

        // 再生終了まで待機
        while (outputDevice.PlaybackState == PlaybackState.Playing)
        {
            await Task.Delay(100);
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
    /// 検査を実行（最終的な保存処理）
    /// </summary>
    public Task<MicrophoneResult> ExecuteAsync(string attachmentPath)
    {
        var result = new MicrophoneResult();

        try
        {
            if (string.IsNullOrEmpty(_tempFilePath) || !File.Exists(_tempFilePath))
            {
                result.Recorded = false;
                result.Error = "録音ファイルが見つかりません";
                return Task.FromResult(result);
            }

            // attachmentsフォルダへ移動
            var fileName = "mic_test.wav";
            var destPath = Path.Combine(attachmentPath, fileName);

            File.Copy(_tempFilePath, destPath, overwrite: true);

            // TEMP削除
            try { File.Delete(_tempFilePath); } catch { /* ignore */ }

            result.Recorded = true;
            result.File = $"attachments/{fileName}";
            result.UserConfirmed = _userConfirmed;

            // デバイス名取得（可能なら）
            try
            {
                var deviceCount = WaveInEvent.DeviceCount;
                if (deviceCount > 0)
                {
                    var capabilities = WaveInEvent.GetCapabilities(0);
                    result.DeviceName = capabilities.ProductName;
                }
            }
            catch
            {
                result.DeviceName = "不明";
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            result.Error = $"マイクアクセス権限エラー: {ex.Message}";
            result.Recorded = false;
        }
        catch (Exception ex)
        {
            result.Error = $"録音失敗: {ex.Message}";
            result.Recorded = false;
        }

        return Task.FromResult(result);
    }
}
