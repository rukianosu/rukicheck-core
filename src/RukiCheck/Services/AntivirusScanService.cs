using System.Diagnostics;
using System.Management;
using RukiCheck.Models;

namespace RukiCheck.Services;

/// <summary>
/// ウイルス対策スキャンサービス
/// Windows Defenderの状態確認とUSBドライブのスキャンを実行
/// </summary>
public class AntivirusScanService
{
    /// <summary>
    /// Windows Defender情報とUSBスキャン結果を取得
    /// </summary>
    public async Task<AntivirusInfo> ScanAsync(string? usbDrivePath = null)
    {
        var antivirusInfo = new AntivirusInfo();

        try
        {
            // Windows Defender情報を取得
            antivirusInfo.WindowsDefender = GetWindowsDefenderInfo();

            // USBドライブのスキャンを実行（パスが指定されている場合）
            if (!string.IsNullOrEmpty(usbDrivePath))
            {
                antivirusInfo.UsbScan = await ScanUsbDriveAsync(usbDrivePath);
            }
        }
        catch (Exception ex)
        {
            antivirusInfo.Error = $"ウイルスチェックエラー: {ex.Message}";
            antivirusInfo.Note = "ウイルス対策情報の取得に失敗しました。";
        }

        return antivirusInfo;
    }

    /// <summary>
    /// Windows Defender情報を取得
    /// </summary>
    private WindowsDefenderInfo GetWindowsDefenderInfo()
    {
        var defenderInfo = new WindowsDefenderInfo();

        try
        {
            // WMI経由でWindows Defenderの状態を取得
            var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Defender");
            scope.Connect();

            // MSFT_MpComputerStatus から状態を取得
            var query = new ObjectQuery("SELECT * FROM MSFT_MpComputerStatus");
            using var searcher = new ManagementObjectSearcher(scope, query);

            foreach (ManagementObject obj in searcher.Get())
            {
                // リアルタイム保護の状態
                var realtimeProtection = obj["RealTimeProtectionEnabled"];
                if (realtimeProtection != null)
                {
                    defenderInfo.RealtimeProtectionEnabled = Convert.ToBoolean(realtimeProtection);
                    defenderInfo.Enabled = defenderInfo.RealtimeProtectionEnabled;
                }

                // 定義ファイルの最終更新日時
                var signatureUpdated = obj["AntivirusSignatureLastUpdated"];
                if (signatureUpdated != null && DateTime.TryParse(signatureUpdated.ToString(), out var signatureDate))
                {
                    defenderInfo.SignatureLastUpdated = signatureDate.ToString("yyyy-MM-dd HH:mm:ss");

                    // 7日以内の更新かチェック
                    defenderInfo.SignatureUpToDate = (DateTime.Now - signatureDate).TotalDays <= 7;
                }

                // 最終スキャン日時
                var lastScan = obj["QuickScanEndTime"];
                if (lastScan != null && DateTime.TryParse(lastScan.ToString(), out var lastScanDate))
                {
                    defenderInfo.LastScanDateTime = lastScanDate.ToString("yyyy-MM-dd HH:mm:ss");
                }

                break; // 最初の結果のみ使用
            }

            // 最近の脅威を確認
            try
            {
                var threatQuery = new ObjectQuery("SELECT * FROM MSFT_MpThreat");
                using var threatSearcher = new ManagementObjectSearcher(scope, threatQuery);

                int threatCount = 0;
                foreach (ManagementObject threat in threatSearcher.Get())
                {
                    threatCount++;
                }

                defenderInfo.RecentThreatsCount = threatCount;
            }
            catch
            {
                // 脅威情報の取得失敗は無視
                defenderInfo.RecentThreatsCount = 0;
            }
        }
        catch (UnauthorizedAccessException)
        {
            defenderInfo.Error = "アクセス拒否（管理者権限が必要な場合があります）";
            defenderInfo.Enabled = false;
        }
        catch (Exception ex)
        {
            defenderInfo.Error = $"Windows Defender情報取得エラー: {ex.Message}";
            defenderInfo.Enabled = false;
        }

        return defenderInfo;
    }

    /// <summary>
    /// USBドライブ（RukiCheckフォルダ）をスキャン
    /// </summary>
    private async Task<UsbScanResult> ScanUsbDriveAsync(string usbDrivePath)
    {
        var scanResult = new UsbScanResult
        {
            ScanPath = usbDrivePath,
            Scanned = false
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // PowerShellでWindows Defenderスキャンを実行
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Start-MpScan -ScanType CustomScan -ScanPath '{usbDrivePath}' -ErrorAction Stop\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            stopwatch.Stop();
            scanResult.ScanDurationSeconds = stopwatch.Elapsed.TotalSeconds;
            scanResult.Scanned = true;

            if (process.ExitCode == 0)
            {
                // スキャン成功、脅威なし
                scanResult.Result = "Clean";
                scanResult.ThreatsFound = 0;
            }
            else
            {
                // エラーが発生した場合
                if (!string.IsNullOrEmpty(error))
                {
                    if (error.Contains("UnauthorizedAccessException") || error.Contains("アクセスが拒否"))
                    {
                        scanResult.Result = "Error";
                        scanResult.Error = "アクセス拒否: 管理者権限が必要です";
                        scanResult.Warning = "USBスキャンには管理者権限が必要な場合があります。手動でスキャンを実行してください。";
                    }
                    else
                    {
                        scanResult.Result = "Error";
                        scanResult.Error = $"スキャンエラー (ExitCode: {process.ExitCode})";
                    }
                }
            }

            // スキャン後に脅威を確認
            await CheckForThreatsAsync(scanResult);
        }
        catch (UnauthorizedAccessException)
        {
            stopwatch.Stop();
            scanResult.ScanDurationSeconds = stopwatch.Elapsed.TotalSeconds;
            scanResult.Result = "Error";
            scanResult.Error = "アクセス拒否: 管理者権限が必要です";
            scanResult.Warning = "USBスキャンには管理者権限が必要な場合があります。";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            scanResult.ScanDurationSeconds = stopwatch.Elapsed.TotalSeconds;
            scanResult.Result = "Error";
            scanResult.Error = $"スキャン失敗: {ex.Message}";
        }

        return scanResult;
    }

    /// <summary>
    /// 最近検出された脅威を確認
    /// </summary>
    private async Task CheckForThreatsAsync(UsbScanResult scanResult)
    {
        await Task.Run(() =>
        {
            try
            {
                var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Defender");
                scope.Connect();

                var query = new ObjectQuery("SELECT * FROM MSFT_MpThreat");
                using var searcher = new ManagementObjectSearcher(scope, query);

                var threats = new List<string>();
                foreach (ManagementObject threat in searcher.Get())
                {
                    try
                    {
                        var threatName = threat["ThreatName"]?.ToString();
                        if (!string.IsNullOrEmpty(threatName))
                        {
                            threats.Add(threatName);
                        }
                    }
                    catch
                    {
                        // 個別の脅威情報取得失敗は無視
                    }
                }

                if (threats.Count > 0)
                {
                    scanResult.Result = "Threat";
                    scanResult.ThreatsFound = threats.Count;
                    scanResult.ThreatNames = threats;
                    scanResult.Warning = $"⚠️ {threats.Count} 個の脅威が検出されました！このUSBメモリは使用しないでください。";
                }
                else if (scanResult.Result != "Error")
                {
                    scanResult.Result = "Clean";
                    scanResult.ThreatsFound = 0;
                }
            }
            catch
            {
                // 脅威情報の確認失敗は無視（スキャン結果を維持）
            }
        });
    }
}
