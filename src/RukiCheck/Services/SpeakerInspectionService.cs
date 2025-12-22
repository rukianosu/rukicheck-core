using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using RukiCheck.Models;

namespace RukiCheck.Services;

/// <summary>
/// スピーカー検査サービス（左右チャンネルテスト音再生）
/// </summary>
public class SpeakerInspectionService : IInspectionService<SpeakerResult>
{
    private bool _leftConfirmed = false;
    private bool _rightConfirmed = false;

    /// <summary>
    /// 左チャンネルテスト音再生
    /// </summary>
    public async Task PlayLeftChannelAsync()
    {
        await PlayTestToneAsync(1.0f, 0.0f); // 左100%, 右0%
    }

    /// <summary>
    /// 右チャンネルテスト音再生
    /// </summary>
    public async Task PlayRightChannelAsync()
    {
        await PlayTestToneAsync(0.0f, 1.0f); // 左0%, 右100%
    }

    /// <summary>
    /// テスト音を再生（ステレオバランス指定）
    /// </summary>
    private async Task PlayTestToneAsync(float leftVolume, float rightVolume)
    {
        const int frequency = 440; // A4 (ラ音)
        const int durationMs = 2000; // 2秒

        var sineWave = new SignalGenerator
        {
            Gain = 0.2,
            Frequency = frequency,
            Type = SignalGeneratorType.Sin
        }.Take(TimeSpan.FromMilliseconds(durationMs));

        // ステレオパンニング
        var stereo = new PanningSampleProvider(sineWave)
        {
            Pan = leftVolume > rightVolume ? -1.0f : 1.0f
        };

        using var outputDevice = new WaveOutEvent();
        outputDevice.Init(stereo);
        outputDevice.Play();

        while (outputDevice.PlaybackState == PlaybackState.Playing)
        {
            await Task.Delay(100);
        }
    }

    /// <summary>
    /// 左チャンネル確認結果を設定
    /// </summary>
    public void SetLeftConfirmation(bool confirmed)
    {
        _leftConfirmed = confirmed;
    }

    /// <summary>
    /// 右チャンネル確認結果を設定
    /// </summary>
    public void SetRightConfirmation(bool confirmed)
    {
        _rightConfirmed = confirmed;
    }

    /// <summary>
    /// 検査結果を生成
    /// </summary>
    public Task<SpeakerResult> ExecuteAsync(string attachmentPath)
    {
        var result = new SpeakerResult
        {
            Left = _leftConfirmed,
            Right = _rightConfirmed
        };

        try
        {
            // デバイス名取得
            var deviceCount = WaveOutEvent.DeviceCount;
            if (deviceCount > 0)
            {
                var capabilities = WaveOutEvent.GetCapabilities(0);
                result.DeviceName = capabilities.ProductName;
            }
        }
        catch
        {
            result.DeviceName = "不明";
        }

        return Task.FromResult(result);
    }
}
