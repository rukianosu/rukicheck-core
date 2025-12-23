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

        using var outputDevice = new WaveOutEvent();

        // デバイスの対応チャンネル数を確認
        var deviceCapabilities = WaveOut.GetCapabilities(0);
        var deviceChannels = deviceCapabilities.Channels;

        // ステレオ音源を生成（SignalGeneratorはデフォルトでステレオ）
        var sineWave = new SignalGenerator
        {
            Gain = 0.2,
            Frequency = frequency,
            Type = SignalGeneratorType.Sin
        }.Take(TimeSpan.FromMilliseconds(durationMs));

        ISampleProvider audioSource;

        // デバイスがステレオ対応の場合、パンニング
        if (deviceChannels >= 2)
        {
            // ステレオパンニング
            var panned = new PanningSampleProvider(sineWave)
            {
                Pan = leftVolume > rightVolume ? -1.0f : 1.0f
            };

            audioSource = panned;
        }
        else
        {
            // モノラルデバイスの場合、ステレオをモノラルに変換
            audioSource = new StereoToMonoSampleProvider(sineWave)
            {
                LeftVolume = leftVolume,
                RightVolume = rightVolume
            };
        }

        outputDevice.Init(audioSource);
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
            var deviceCount = WaveOut.DeviceCount;
            if (deviceCount > 0)
            {
                var capabilities = WaveOut.GetCapabilities(0);
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
