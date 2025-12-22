using RukiCheck.Models;

namespace RukiCheck.Services;

/// <summary>
/// CPU簡易負荷テストサービス
/// </summary>
public class CpuInspectionService : IInspectionService<CpuResult>
{
    /// <summary>
    /// CPU負荷テストを実行（30〜60秒）
    /// </summary>
    public async Task<CpuResult> ExecuteAsync(string attachmentPath)
    {
        var result = new CpuResult
        {
            DurationSec = 60
        };

        try
        {
            result.StressTest = "running";

            var startTime = DateTime.Now;
            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            // 並列計算負荷（全論理コア使用）
            var task = Task.Run(() =>
            {
                Parallel.For(0, Environment.ProcessorCount, new ParallelOptions { CancellationToken = token }, i =>
                {
                    var random = new Random();
                    double sum = 0;

                    while (!token.IsCancellationRequested)
                    {
                        // 計算負荷（素数判定的な処理）
                        for (int n = 2; n < 10000; n++)
                        {
                            bool isPrime = true;
                            for (int j = 2; j * j <= n; j++)
                            {
                                if (n % j == 0)
                                {
                                    isPrime = false;
                                    break;
                                }
                            }
                            if (isPrime)
                                sum += Math.Sqrt(n);
                        }

                        // キャンセル確認
                        if (token.IsCancellationRequested)
                            break;
                    }
                });
            }, token);

            // 60秒待機
            await Task.Delay(result.DurationSec * 1000);

            // 停止
            cancellationTokenSource.Cancel();

            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // 正常なキャンセル
            }

            result.StressTest = "completed";
            result.Abnormal = false;
            result.Note = $"CPU負荷テスト {result.DurationSec}秒間完了。フリーズや異常停止は検出されませんでした。";

            // 温度取得（Windows標準APIでは困難なため省略）
            result.Temperature = null;
        }
        catch (Exception ex)
        {
            result.StressTest = "failed";
            result.Abnormal = true;
            result.Error = $"CPU負荷テスト中に異常発生: {ex.Message}";
        }

        return result;
    }
}
