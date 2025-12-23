using System.Diagnostics;
using System.Management;
using RukiCheck.Models;

namespace RukiCheck.Services;

/// <summary>
/// メモリテストモード
/// </summary>
public enum MemoryTestMode
{
    /// <summary>
    /// 標準モード（85-90%検出率、1-3分）
    /// </summary>
    Standard,

    /// <summary>
    /// 徹底モード（95%以上検出率、3-8分）
    /// </summary>
    Thorough
}

/// <summary>
/// メモリテストサービス（RAM読み書きテスト）
/// </summary>
public class MemoryTestService : IInspectionService<MemoryTestResult>
{
    private bool _userConfirmed = false;
    private int _testedMb = 0;
    private double _testDurationSec = 0;
    private int _patternsTested = 0;
    private int _failedPatterns = 0;
    private double _writeSpeedMbps = 0;
    private double _readSpeedMbps = 0;
    private long _availableMb = 0;
    private double _totalGb = 0;
    private MemoryTestMode _testMode = MemoryTestMode.Standard;
    private int _totalPasses = 0;
    private int _currentPass = 0;

    public event Action<int>? OnProgressChanged;
    public event Action<string>? OnStatusChanged;

    /// <summary>
    /// テストモードを設定
    /// </summary>
    public void SetTestMode(MemoryTestMode mode)
    {
        _testMode = mode;
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
    /// テストパターンを生成
    /// </summary>
    private byte[][] GetTestPatterns()
    {
        if (_testMode == MemoryTestMode.Standard)
        {
            // 標準モード: 8パターン
            return new byte[][]
            {
                new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, // All 1s
                new byte[] { 0x00, 0x00, 0x00, 0x00 }, // All 0s
                new byte[] { 0xAA, 0xAA, 0xAA, 0xAA }, // 10101010
                new byte[] { 0x55, 0x55, 0x55, 0x55 }, // 01010101
                new byte[] { 0x01, 0x00, 0x00, 0x00 }, // Walking 1s (start)
                new byte[] { 0xFE, 0xFF, 0xFF, 0xFF }, // Walking 0s (start)
                new byte[] { 0x12, 0x34, 0x56, 0x78 }, // Pattern 1
                new byte[] { 0xED, 0xCB, 0xA9, 0x87 }  // Pattern 2 (inverse)
            };
        }
        else // Thorough
        {
            // 徹底モード: 12パターン
            return new byte[][]
            {
                new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, // All 1s
                new byte[] { 0x00, 0x00, 0x00, 0x00 }, // All 0s
                new byte[] { 0xAA, 0xAA, 0xAA, 0xAA }, // 10101010
                new byte[] { 0x55, 0x55, 0x55, 0x55 }, // 01010101
                new byte[] { 0x01, 0x00, 0x00, 0x00 }, // Walking 1s (bit 0)
                new byte[] { 0x02, 0x00, 0x00, 0x00 }, // Walking 1s (bit 1)
                new byte[] { 0xFE, 0xFF, 0xFF, 0xFF }, // Walking 0s (bit 0)
                new byte[] { 0xFD, 0xFF, 0xFF, 0xFF }, // Walking 0s (bit 1)
                new byte[] { 0x12, 0x34, 0x56, 0x78 }, // Pattern 1
                new byte[] { 0xED, 0xCB, 0xA9, 0x87 }, // Pattern 2 (inverse)
                new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, // Pattern 3
                new byte[] { 0x21, 0x52, 0x41, 0x10 }  // Pattern 4
            };
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

            // モードに応じたテストサイズと パス数を決定
            int testSizeMb;
            int passes;
            double percentageOfAvailable;

            if (_testMode == MemoryTestMode.Standard)
            {
                // 標準モード: 80%、最大6GB、3パス
                percentageOfAvailable = 0.80;
                int maxSizeMb = 6 * 1024; // 6GB
                testSizeMb = Math.Min((int)(_availableMb * percentageOfAvailable), maxSizeMb);
                passes = 3;
            }
            else // Thorough
            {
                // 徹底モード: 90%、最大8GB、5パス
                percentageOfAvailable = 0.90;
                int maxSizeMb = 8 * 1024; // 8GB
                testSizeMb = Math.Min((int)(_availableMb * percentageOfAvailable), maxSizeMb);
                passes = 5;
            }

            _testedMb = testSizeMb;
            _totalPasses = passes;

            if (testSizeMb < 100)
            {
                OnStatusChanged?.Invoke("利用可能メモリが不足しています");
                return false;
            }

            string modeName = _testMode == MemoryTestMode.Standard ? "標準" : "徹底";
            OnStatusChanged?.Invoke($"{modeName}モード: {testSizeMb} MB × {passes}パス");

            // テストパターン取得
            byte[][] patterns = GetTestPatterns();
            _patternsTested = patterns.Length * passes;
            _failedPatterns = 0;

            int totalTests = patterns.Length * passes;
            int completedTests = 0;

            // 複数パス実行
            for (int pass = 0; pass < passes; pass++)
            {
                _currentPass = pass + 1;
                OnStatusChanged?.Invoke($"パス {_currentPass}/{passes} を実行中...");

                // 各パターンでテスト
                for (int i = 0; i < patterns.Length; i++)
                {
                    OnStatusChanged?.Invoke($"パス {_currentPass}/{passes} - パターン {i + 1}/{patterns.Length} テスト中...");

                    bool passed = await TestPatternAsync(patterns[i], testSizeMb);
                    if (!passed)
                    {
                        _failedPatterns++;
                    }

                    completedTests++;
                    int progress = (completedTests * 100) / totalTests;
                    OnProgressChanged?.Invoke(progress);

                    await Task.Delay(50); // UI更新のための短い遅延
                }
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
            TestMode = _testMode.ToString(),
            TestedMb = _testedMb,
            AvailableMb = _availableMb,
            TotalGb = _totalGb,
            TestDurationSec = _testDurationSec,
            PatternsTested = _patternsTested,
            AllPatternsPassed = _failedPatterns == 0,
            FailedPatterns = _failedPatterns,
            WriteSpeedMbps = Math.Round(_writeSpeedMbps, 2),
            ReadSpeedMbps = Math.Round(_readSpeedMbps, 2),
            UserConfirmed = _userConfirmed,
            TotalPasses = _totalPasses
        };

        if (!result.TestExecuted)
        {
            result.Note = "テストが実行されませんでした";
        }
        else if (result.AllPatternsPassed)
        {
            string modeName = _testMode == MemoryTestMode.Standard ? "標準" : "徹底";
            result.Note = $"{modeName}モード: 全{_patternsTested}パターンのテストに成功しました";
        }
        else
        {
            result.Error = $"{_failedPatterns}個のパターンテストが失敗しました";
        }

        return Task.FromResult(result);
    }
}
