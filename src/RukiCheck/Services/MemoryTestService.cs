using System.Diagnostics;
using System.Management;
using RukiCheck.Models;

namespace RukiCheck.Services;

/// <summary>
/// メモリテストサービス（RAM読み書きテスト）
/// </summary>
public class MemoryTestService : IInspectionService<MemoryTestResult>
{
    private bool _userConfirmed = false;
    private int _testedMb = 512; // デフォルト512MB
    private double _testDurationSec = 0;
    private int _patternsTested = 0;
    private int _failedPatterns = 0;
    private double _writeSpeedMbps = 0;
    private double _readSpeedMbps = 0;
    private long _availableMb = 0;
    private double _totalGb = 0;

    public event Action<int>? OnProgressChanged;
    public event Action<string>? OnStatusChanged;

    /// <summary>
    /// テストするメモリサイズを設定（MB）
    /// </summary>
    public void SetTestSize(int sizeMb)
    {
        _testedMb = sizeMb;
    }

    /// <summary>
    /// メモリ情報を取得
    /// </summary>
    private void GetMemoryInfo()
    {
        try
        {
            // 利用可能な物理メモリ
            using var searcher = new ManagementObjectSearcher("SELECT FreePhysicalMemory, TotalVisibleMemorySize FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                var freeKb = Convert.ToInt64(obj["FreePhysicalMemory"]);
                var totalKb = Convert.ToInt64(obj["TotalVisibleMemorySize"]);
                _availableMb = freeKb / 1024;
                _totalGb = totalKb / (1024.0 * 1024.0);
                break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"メモリ情報取得エラー: {ex.Message}");
        }
    }

    /// <summary>
    /// メモリテストを実行（非同期）
    /// </summary>
    public async Task<bool> RunMemoryTestAsync()
    {
        GetMemoryInfo();

        try
        {
            var stopwatch = Stopwatch.StartNew();
            OnStatusChanged?.Invoke("メモリテスト準備中...");

            // テストサイズの調整（利用可能メモリの50%以下、最大2GB）
            int testSizeMb = Math.Min(_testedMb, (int)(_availableMb * 0.5));
            testSizeMb = Math.Min(testSizeMb, 2048);
            _testedMb = testSizeMb;

            if (testSizeMb < 100)
            {
                OnStatusChanged?.Invoke("利用可能メモリが不足しています");
                return false;
            }

            OnStatusChanged?.Invoke($"テストサイズ: {testSizeMb} MB");

            // テストパターン（4種類）
            byte[][] patterns = new byte[][]
            {
                new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, // All 1s
                new byte[] { 0x00, 0x00, 0x00, 0x00 }, // All 0s
                new byte[] { 0xAA, 0xAA, 0xAA, 0xAA }, // 10101010
                new byte[] { 0x55, 0x55, 0x55, 0x55 }  // 01010101
            };

            _patternsTested = patterns.Length;
            _failedPatterns = 0;

            int progressStep = 100 / patterns.Length;

            // 各パターンでテスト
            for (int i = 0; i < patterns.Length; i++)
            {
                OnStatusChanged?.Invoke($"パターン {i + 1}/{patterns.Length} テスト中...");

                bool passed = await TestPatternAsync(patterns[i], testSizeMb);
                if (!passed)
                {
                    _failedPatterns++;
                }

                OnProgressChanged?.Invoke((i + 1) * progressStep);
                await Task.Delay(100); // UI更新のための短い遅延
            }

            stopwatch.Stop();
            _testDurationSec = stopwatch.Elapsed.TotalSeconds;

            OnStatusChanged?.Invoke("メモリテスト完了");
            OnProgressChanged?.Invoke(100);

            return _failedPatterns == 0;
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"エラー: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 特定のパターンでメモリをテスト
    /// </summary>
    private async Task<bool> TestPatternAsync(byte[] pattern, int sizeMb)
    {
        try
        {
            // メモリ確保（配列サイズ = sizeMb * 1024 * 1024 / 4）
            int arraySize = sizeMb * 1024 * 256; // 256K integers = 1MB
            int[] buffer = new int[arraySize];

            // パターンをintに変換
            int patternInt = BitConverter.ToInt32(pattern, 0);

            // 書き込み速度測定
            var writeStopwatch = Stopwatch.StartNew();
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = patternInt;
            }
            writeStopwatch.Stop();
            _writeSpeedMbps = sizeMb / writeStopwatch.Elapsed.TotalSeconds;

            await Task.Delay(50); // メモリ書き込み後の短い待機

            // 読み込みと検証速度測定
            var readStopwatch = Stopwatch.StartNew();
            bool verified = true;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] != patternInt)
                {
                    verified = false;
                    break;
                }
            }
            readStopwatch.Stop();
            _readSpeedMbps = sizeMb / readStopwatch.Elapsed.TotalSeconds;

            // バッファクリア（GC用）
            Array.Clear(buffer, 0, buffer.Length);
            GC.Collect();
            GC.WaitForPendingFinalizers();

            return verified;
        }
        catch (OutOfMemoryException)
        {
            OnStatusChanged?.Invoke("メモリ不足エラー");
            return false;
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"パターンテストエラー: {ex.Message}");
            return false;
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
    public Task<MemoryTestResult> ExecuteAsync(string attachmentPath)
    {
        GetMemoryInfo();

        var result = new MemoryTestResult
        {
            TestExecuted = _patternsTested > 0,
            TestedMb = _testedMb,
            AvailableMb = _availableMb,
            TotalGb = _totalGb,
            TestDurationSec = _testDurationSec,
            PatternsTested = _patternsTested,
            AllPatternsPassed = _failedPatterns == 0,
            FailedPatterns = _failedPatterns,
            WriteSpeedMbps = Math.Round(_writeSpeedMbps, 2),
            ReadSpeedMbps = Math.Round(_readSpeedMbps, 2),
            UserConfirmed = _userConfirmed
        };

        if (!result.TestExecuted)
        {
            result.Note = "テストが実行されませんでした";
        }
        else if (result.AllPatternsPassed)
        {
            result.Note = $"全{_patternsTested}パターンのテストに成功しました";
        }
        else
        {
            result.Error = $"{_failedPatterns}個のパターンテストが失敗しました";
        }

        return Task.FromResult(result);
    }
}
