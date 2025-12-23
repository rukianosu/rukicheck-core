using System.Management;
using RukiCheck.Models;

namespace RukiCheck.Services;

/// <summary>
/// ハードウェア情報収集サービス
/// WMI (Windows Management Instrumentation) を使用してシステム情報を取得
/// </summary>
public class HardwareInfoService : IInspectionService<HardwareInfoResult>
{
    /// <summary>
    /// ハードウェア情報を収集
    /// </summary>
    public Task<HardwareInfoResult> ExecuteAsync(string attachmentPath)
    {
        var result = new HardwareInfoResult();

        try
        {
            // メモリ情報を収集
            result.Memory = GetMemoryInfo();

            // CPU情報を収集
            result.Cpu = GetCpuInfo();

            // GPU情報を収集
            result.Gpus = GetGpuInfo();

            // BitLocker情報を収集
            result.BitLocker = GetBitLockerInfo();

            // ストレージヘルス情報を収集
            result.StorageHealth = GetStorageHealthInfo();
        }
        catch (Exception ex)
        {
            // エラーが発生した場合でも部分的な情報を返す
            Console.WriteLine($"ハードウェア情報収集エラー: {ex.Message}");
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// メモリ情報を取得
    /// </summary>
    private MemoryInfo GetMemoryInfo()
    {
        var memoryInfo = new MemoryInfo();

        try
        {
            // Win32_PhysicalMemory から物理メモリ情報を取得
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            var modules = new List<MemoryModule>();
            ulong totalCapacity = 0;

            foreach (ManagementObject obj in searcher.Get())
            {
                var capacity = Convert.ToUInt64(obj["Capacity"]);
                totalCapacity += capacity;

                var module = new MemoryModule
                {
                    CapacityGb = Math.Round(capacity / 1024.0 / 1024.0 / 1024.0, 2),
                    Manufacturer = obj["Manufacturer"]?.ToString()?.Trim() ?? "不明",
                    SpeedMhz = Convert.ToInt32(obj["Speed"] ?? 0),
                    PartNumber = obj["PartNumber"]?.ToString()?.Trim() ?? ""
                };

                modules.Add(module);
            }

            memoryInfo.TotalGb = Math.Round(totalCapacity / 1024.0 / 1024.0 / 1024.0, 2);
            memoryInfo.Slots = modules.Count;
            memoryInfo.Modules = modules;

            // 最初のモジュールから速度とタイプを取得
            if (modules.Count > 0)
            {
                memoryInfo.SpeedMhz = modules[0].SpeedMhz;
            }

            // Win32_PhysicalMemory の MemoryType から DDR 世代を判定
            using var typeSearcher = new ManagementObjectSearcher("SELECT MemoryType, SMBIOSMemoryType FROM Win32_PhysicalMemory");
            foreach (ManagementObject obj in typeSearcher.Get())
            {
                var smbiosType = Convert.ToInt32(obj["SMBIOSMemoryType"] ?? 0);
                memoryInfo.Type = GetMemoryTypeString(smbiosType);
                break; // 最初のモジュールのタイプを使用
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"メモリ情報取得エラー: {ex.Message}");
            memoryInfo.Type = "不明";
        }

        return memoryInfo;
    }

    /// <summary>
    /// CPU情報を取得
    /// </summary>
    private CpuInfo GetCpuInfo()
    {
        var cpuInfo = new CpuInfo();

        try
        {
            // Win32_Processor から CPU 情報を取得
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");

            foreach (ManagementObject obj in searcher.Get())
            {
                cpuInfo.Name = obj["Name"]?.ToString()?.Trim() ?? "不明";
                cpuInfo.Manufacturer = obj["Manufacturer"]?.ToString()?.Trim() ?? "不明";
                cpuInfo.Cores = Convert.ToInt32(obj["NumberOfCores"] ?? 0);
                cpuInfo.LogicalProcessors = Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? 0);
                cpuInfo.MaxClockMhz = Convert.ToInt32(obj["MaxClockSpeed"] ?? 0);

                // アーキテクチャを取得
                var architecture = Convert.ToInt32(obj["Architecture"] ?? 0);
                cpuInfo.Architecture = GetArchitectureString(architecture);

                break; // 最初の CPU のみ
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CPU情報取得エラー: {ex.Message}");
            cpuInfo.Name = "取得失敗";
        }

        return cpuInfo;
    }

    /// <summary>
    /// GPU情報を取得
    /// </summary>
    private List<GpuInfo> GetGpuInfo()
    {
        var gpus = new List<GpuInfo>();

        try
        {
            // Win32_VideoController から GPU 情報を取得
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

            foreach (ManagementObject obj in searcher.Get())
            {
                var gpu = new GpuInfo
                {
                    Name = obj["Name"]?.ToString()?.Trim() ?? "不明",
                    AdapterManufacturer = obj["AdapterCompatibility"]?.ToString()?.Trim() ?? "不明",
                    VideoProcessor = obj["VideoProcessor"]?.ToString()?.Trim() ?? "不明",
                    DriverVersion = obj["DriverVersion"]?.ToString()?.Trim() ?? "不明"
                };

                // VRAM容量を取得（バイト単位）
                var adapterRam = obj["AdapterRAM"];
                if (adapterRam != null)
                {
                    var vramBytes = Convert.ToUInt64(adapterRam);
                    gpu.VramGb = Math.Round(vramBytes / 1024.0 / 1024.0 / 1024.0, 2);
                }

                gpus.Add(gpu);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GPU情報取得エラー: {ex.Message}");
            gpus.Add(new GpuInfo { Name = "取得失敗" });
        }

        return gpus;
    }

    /// <summary>
    /// SMBIOS メモリタイプコードから文字列を取得
    /// https://www.dmtf.org/sites/default/files/standards/documents/DSP0134_3.6.0.pdf
    /// </summary>
    private string GetMemoryTypeString(int smbiosType)
    {
        return smbiosType switch
        {
            20 => "DDR",
            21 => "DDR2",
            24 => "DDR3",
            26 => "DDR4",
            34 => "DDR5",
            _ => $"Unknown ({smbiosType})"
        };
    }

    /// <summary>
    /// CPUアーキテクチャコードから文字列を取得
    /// </summary>
    private string GetArchitectureString(int architecture)
    {
        return architecture switch
        {
            0 => "x86",
            1 => "MIPS",
            2 => "Alpha",
            3 => "PowerPC",
            5 => "ARM",
            6 => "ia64",
            9 => "x64",
            12 => "ARM64",
            _ => $"Unknown ({architecture})"
        };
    }

    /// <summary>
    /// BitLocker情報を取得
    /// </summary>
    private BitLockerInfo GetBitLockerInfo()
    {
        var bitlockerInfo = new BitLockerInfo();

        try
        {
            // Win32_EncryptableVolume から BitLocker 情報を取得
            // 注: このWMIクラスは管理者権限が必要な場合があります
            var scope = new ManagementScope(@"\\.\root\CIMV2\Security\MicrosoftVolumeEncryption");

            try
            {
                scope.Connect();
            }
            catch (UnauthorizedAccessException)
            {
                bitlockerInfo.Error = "アクセス拒否";
                bitlockerInfo.Note = "BitLocker情報の取得には管理者権限が必要です。管理者として実行してください。";
                return bitlockerInfo;
            }
            catch (Exception ex)
            {
                bitlockerInfo.Error = $"接続エラー: {ex.Message}";
                bitlockerInfo.Note = "BitLocker機能が利用できないか、サポートされていません。";
                return bitlockerInfo;
            }

            var query = new ObjectQuery("SELECT * FROM Win32_EncryptableVolume");
            using var searcher = new ManagementObjectSearcher(scope, query);

            foreach (ManagementObject vol in searcher.Get())
            {
                try
                {
                    var volume = new BitLockerVolume();

                    // ドライブレター取得
                    volume.DriveLetter = vol["DriveLetter"]?.ToString() ?? "不明";

                    // 保護状態を取得
                    var protectionStatus = Convert.ToInt32(vol["ProtectionStatus"] ?? 0);
                    volume.ProtectionStatus = protectionStatus switch
                    {
                        0 => "保護オフ",
                        1 => "保護オン",
                        2 => "不明",
                        _ => $"状態 {protectionStatus}"
                    };
                    volume.IsEncrypted = protectionStatus == 1;

                    // 暗号化率を取得
                    try
                    {
                        var conversionStatus = Convert.ToInt32(vol["ConversionStatus"] ?? 0);
                        volume.EncryptionPercentage = conversionStatus == 1 ? 100 : 0;
                    }
                    catch
                    {
                        volume.EncryptionPercentage = 0;
                    }

                    // 回復キーの取得を試みる
                    try
                    {
                        var getKeyProtectorsMethod = vol.GetMethodParameters("GetKeyProtectors");
                        getKeyProtectorsMethod["KeyProtectorType"] = 3; // RecoveryPassword = 3

                        var result = vol.InvokeMethod("GetKeyProtectors", getKeyProtectorsMethod, null);

                        if (result != null && result["VolumeKeyProtectorID"] != null)
                        {
                            var keyProtectorIds = (string[])result["VolumeKeyProtectorID"];

                            foreach (var keyId in keyProtectorIds)
                            {
                                try
                                {
                                    var getKeyMethod = vol.GetMethodParameters("GetKeyProtectorNumericalPassword");
                                    getKeyMethod["VolumeKeyProtectorID"] = keyId;

                                    var keyResult = vol.InvokeMethod("GetKeyProtectorNumericalPassword", getKeyMethod, null);

                                    if (keyResult != null && keyResult["NumericalPassword"] != null)
                                    {
                                        volume.RecoveryKeys.Add(keyResult["NumericalPassword"].ToString()!);
                                    }
                                }
                                catch (UnauthorizedAccessException)
                                {
                                    volume.Error = "回復キーの取得には管理者権限が必要です";
                                }
                                catch
                                {
                                    // キー取得失敗は無視
                                }
                            }
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        volume.Error = "回復キーの取得には管理者権限が必要です";
                    }
                    catch (Exception ex)
                    {
                        volume.Error = $"回復キー取得エラー: {ex.Message}";
                    }

                    bitlockerInfo.Volumes.Add(volume);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"BitLockerボリューム情報取得エラー: {ex.Message}");
                }
            }

            if (bitlockerInfo.Volumes.Count == 0)
            {
                bitlockerInfo.Note = "BitLockerで暗号化されたボリュームが見つかりませんでした。";
            }
        }
        catch (UnauthorizedAccessException)
        {
            bitlockerInfo.Error = "アクセス拒否";
            bitlockerInfo.Note = "BitLocker情報の取得には管理者権限が必要です。";
        }
        catch (Exception ex)
        {
            bitlockerInfo.Error = $"取得失敗: {ex.Message}";
            bitlockerInfo.Note = "BitLocker機能が利用できないか、サポートされていません。";
        }

        return bitlockerInfo;
    }

    /// <summary>
    /// ストレージヘルス情報を取得（SMART/NVMe）
    /// </summary>
    private List<StorageHealthInfo> GetStorageHealthInfo()
    {
        var storageList = new List<StorageHealthInfo>();

        try
        {
            // Win32_DiskDrive から物理ドライブ情報を取得
            using var diskDriveSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");

            int driveIndex = 0;
            foreach (ManagementObject diskDrive in diskDriveSearcher.Get())
            {
                try
                {
                    var storage = new StorageHealthInfo();

                    // 基本情報
                    storage.DriveNumber = driveIndex++;
                    storage.Model = diskDrive["Model"]?.ToString()?.Trim() ?? "不明";
                    storage.SerialNumber = diskDrive["SerialNumber"]?.ToString()?.Trim() ?? "不明";

                    // インターフェースタイプ
                    var interfaceType = diskDrive["InterfaceType"]?.ToString() ?? "不明";
                    storage.InterfaceType = interfaceType;

                    // メディアタイプ
                    var mediaType = diskDrive["MediaType"]?.ToString() ?? "";
                    if (mediaType.Contains("SSD") || storage.Model.ToUpper().Contains("SSD"))
                    {
                        storage.MediaType = "SSD";
                    }
                    else if (mediaType.Contains("HDD") || mediaType.Contains("Fixed"))
                    {
                        storage.MediaType = "HDD";
                    }
                    else
                    {
                        storage.MediaType = "不明";
                    }

                    // 容量（バイト → GB）
                    var sizeBytes = diskDrive["Size"];
                    if (sizeBytes != null)
                    {
                        var size = Convert.ToUInt64(sizeBytes);
                        storage.CapacityGb = Math.Round(size / 1024.0 / 1024.0 / 1024.0, 2);
                    }

                    // MSFT_PhysicalDisk から追加情報を取得（Windows 8以降）
                    try
                    {
                        var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Storage");
                        scope.Connect();

                        // DeviceIDからドライブを特定
                        var deviceId = diskDrive["DeviceID"]?.ToString();
                        if (!string.IsNullOrEmpty(deviceId))
                        {
                            // DeviceID は "\\.\PHYSICALDRIVE0" の形式
                            var driveNumberStr = deviceId.Replace(@"\\.\PHYSICALDRIVE", "");

                            var physDiskQuery = new ObjectQuery($"SELECT * FROM MSFT_PhysicalDisk WHERE DeviceId = '{driveNumberStr}'");
                            using var physDiskSearcher = new ManagementObjectSearcher(scope, physDiskQuery);

                            foreach (ManagementObject physDisk in physDiskSearcher.Get())
                            {
                                // ヘルスステータス
                                var healthStatus = Convert.ToInt32(physDisk["HealthStatus"] ?? 0);
                                storage.HealthStatus = healthStatus switch
                                {
                                    0 => "OK",
                                    1 => "Warning",
                                    2 => "Critical",
                                    _ => "Unknown"
                                };

                                // メディアタイプの再判定（より正確）
                                var mediaTypeCode = Convert.ToInt32(physDisk["MediaType"] ?? 0);
                                storage.MediaType = mediaTypeCode switch
                                {
                                    3 => "HDD",
                                    4 => "SSD",
                                    5 => "SCM",
                                    _ => storage.MediaType
                                };

                                // 使用状況
                                var usage = Convert.ToInt32(physDisk["Usage"] ?? 0);
                                if (usage == 1)
                                {
                                    storage.Note = "自動選択プールのメンバー";
                                }

                                break; // 最初の一致で十分
                            }

                            // MSFT_StorageReliabilityCounter から詳細ヘルス情報を取得
                            var reliabilityQuery = new ObjectQuery($"SELECT * FROM MSFT_StorageReliabilityCounter WHERE DeviceId = '{driveNumberStr}'");
                            using var reliabilitySearcher = new ManagementObjectSearcher(scope, reliabilityQuery);

                            foreach (ManagementObject reliability in reliabilitySearcher.Get())
                            {
                                // 温度
                                var temperature = reliability["Temperature"];
                                if (temperature != null && Convert.ToInt32(temperature) > 0)
                                {
                                    storage.TemperatureCelsius = Convert.ToInt32(temperature);
                                }

                                // 通電時間
                                var powerOnHours = reliability["PowerOnHours"];
                                if (powerOnHours != null)
                                {
                                    storage.PowerOnHours = Convert.ToInt64(powerOnHours);
                                }

                                // SSDの場合の残り寿命（Wear）
                                var wear = reliability["Wear"];
                                if (wear != null && Convert.ToInt32(wear) >= 0)
                                {
                                    var wearPercent = Convert.ToInt32(wear);
                                    storage.RemainingLifePercent = 100 - wearPercent; // Wearは消耗率なので反転
                                }

                                // 総書き込み量（セクター数 × 512バイト → GB）
                                var writeCommands = reliability["WriteCommandsCount"];
                                if (writeCommands != null)
                                {
                                    var writeSectors = Convert.ToInt64(writeCommands);
                                    storage.TotalBytesWrittenGb = Math.Round(writeSectors * 512.0 / 1024.0 / 1024.0 / 1024.0, 2);
                                }

                                // 総読み込み量（セクター数 × 512バイト → GB）
                                var readCommands = reliability["ReadCommandsCount"];
                                if (readCommands != null)
                                {
                                    var readSectors = Convert.ToInt64(readCommands);
                                    storage.TotalBytesReadGb = Math.Round(readSectors * 512.0 / 1024.0 / 1024.0 / 1024.0, 2);
                                }

                                break; // 最初の一致で十分
                            }
                        }
                    }
                    catch (Exception msftEx)
                    {
                        // MSFT_PhysicalDisk へのアクセス失敗（古いWindowsやアクセス権限不足）
                        storage.Note = $"詳細情報取得不可: {msftEx.Message}";
                        storage.HealthStatus = "Unknown";
                    }

                    // SMARTデータの簡易取得を試みる（WMI経由では制限あり）
                    // 注：完全なSMART情報取得には専用ドライバやライブラリが必要
                    try
                    {
                        var smartQuery = $"SELECT * FROM MSStorageDriver_FailurePredictStatus WHERE InstanceName LIKE '%{storage.Model}%'";
                        using var smartSearcher = new ManagementObjectSearcher(@"\\.\root\wmi", smartQuery);

                        foreach (ManagementObject smart in smartSearcher.Get())
                        {
                            var predictFailure = Convert.ToBoolean(smart["PredictFailure"] ?? false);
                            if (predictFailure)
                            {
                                storage.HealthStatus = "Critical";
                                storage.CriticalWarning = "SMART障害予測が検出されました";
                            }

                            break;
                        }
                    }
                    catch
                    {
                        // SMART情報取得失敗は無視（多くのシステムで利用不可）
                    }

                    storageList.Add(storage);
                }
                catch (Exception driveEx)
                {
                    Console.WriteLine($"ドライブ情報取得エラー: {driveEx.Message}");

                    // エラーでも最低限の情報は追加
                    storageList.Add(new StorageHealthInfo
                    {
                        DriveNumber = driveIndex++,
                        Model = "取得失敗",
                        Error = driveEx.Message
                    });
                }
            }

            if (storageList.Count == 0)
            {
                storageList.Add(new StorageHealthInfo
                {
                    DriveNumber = 0,
                    Model = "ストレージが検出されませんでした",
                    Note = "物理ドライブ情報を取得できませんでした"
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ストレージヘルス情報取得エラー: {ex.Message}");

            storageList.Add(new StorageHealthInfo
            {
                DriveNumber = 0,
                Model = "取得失敗",
                Error = $"ストレージ情報の取得に失敗しました: {ex.Message}",
                Note = "WMIアクセスに失敗しました。管理者権限が必要な場合があります。"
            });
        }

        return storageList;
    }
}
