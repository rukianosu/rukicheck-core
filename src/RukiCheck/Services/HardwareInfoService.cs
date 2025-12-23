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
            // システム情報を収集（メーカー、型番、シリアルナンバー）
            result.System = GetSystemInfo();

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

            // Windowsライセンス認証情報を収集
            result.WindowsLicense = GetWindowsLicenseInfo();
        }
        catch (Exception ex)
        {
            // エラーが発生した場合でも部分的な情報を返す
            Console.WriteLine($"ハードウェア情報収集エラー: {ex.Message}");
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// システム情報を取得（メーカー、型番、シリアルナンバー）
    /// </summary>
    private SystemInfo GetSystemInfo()
    {
        var systemInfo = new SystemInfo();

        try
        {
            // Win32_ComputerSystem からメーカーと型番を取得
            using var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Model FROM Win32_ComputerSystem");

            foreach (ManagementObject obj in searcher.Get())
            {
                systemInfo.Manufacturer = obj["Manufacturer"]?.ToString()?.Trim() ?? "不明";
                systemInfo.Model = obj["Model"]?.ToString()?.Trim() ?? "不明";
                break; // 最初の結果のみ使用
            }

            // Win32_BIOS からシリアルナンバーとBIOSバージョンを取得
            using var biosSearcher = new ManagementObjectSearcher("SELECT SerialNumber, SMBIOSBIOSVersion FROM Win32_BIOS");

            foreach (ManagementObject bios in biosSearcher.Get())
            {
                var serialNumber = bios["SerialNumber"]?.ToString()?.Trim() ?? "";

                // シリアルナンバーが取得できない場合やデフォルト値の場合
                if (string.IsNullOrEmpty(serialNumber) ||
                    serialNumber.Equals("To Be Filled By O.E.M.", StringComparison.OrdinalIgnoreCase) ||
                    serialNumber.Equals("Default string", StringComparison.OrdinalIgnoreCase) ||
                    serialNumber.Equals("System Serial Number", StringComparison.OrdinalIgnoreCase))
                {
                    systemInfo.SerialNumber = "取得不可";
                }
                else
                {
                    systemInfo.SerialNumber = serialNumber;
                }

                systemInfo.BiosVersion = bios["SMBIOSBIOSVersion"]?.ToString()?.Trim() ?? "不明";
                break; // 最初の結果のみ使用
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"システム情報取得エラー: {ex.Message}");
            systemInfo.Manufacturer = "取得失敗";
            systemInfo.Model = "取得失敗";
            systemInfo.SerialNumber = "取得失敗";
            systemInfo.BiosVersion = "取得失敗";
        }

        return systemInfo;
    }

    /// <summary>
    /// メモリ情報を取得
    /// </summary>
    private MemoryInfo GetMemoryInfo()
    {
        var memoryInfo = new MemoryInfo();

        try
        {
            // Win32_PhysicalMemoryArray から物理的な全スロット数を取得
            try
            {
                using var arraySearcher = new ManagementObjectSearcher("SELECT MemoryDevices FROM Win32_PhysicalMemoryArray");
                foreach (ManagementObject array in arraySearcher.Get())
                {
                    var memoryDevices = array["MemoryDevices"];
                    if (memoryDevices != null)
                    {
                        memoryInfo.TotalSlots = Convert.ToInt32(memoryDevices);
                        break; // 通常は1つのメモリアレイのみ
                    }
                }
            }
            catch (Exception slotEx)
            {
                Console.WriteLine($"全スロット数取得エラー: {slotEx.Message}");
                memoryInfo.TotalSlots = 0; // 取得失敗時は0
            }

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
            memoryInfo.InstalledModules = modules.Count;
            memoryInfo.Slots = modules.Count; // 後方互換性のため
            memoryInfo.Modules = modules;

            // 全スロット数が取得できなかった場合は、実装数と同じにする
            if (memoryInfo.TotalSlots == 0)
            {
                memoryInfo.TotalSlots = modules.Count;
            }

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

                    // SMARTデータの取得を試みる
                    try
                    {
                        // まず障害予測ステータスをチェック
                        var smartStatusQuery = $"SELECT * FROM MSStorageDriver_FailurePredictStatus WHERE InstanceName LIKE '%{storage.Model}%'";
                        using var statusSearcher = new ManagementObjectSearcher(@"\\.\root\wmi", smartStatusQuery);

                        foreach (ManagementObject statusObj in statusSearcher.Get())
                        {
                            var predictFailure = Convert.ToBoolean(statusObj["PredictFailure"] ?? false);
                            if (predictFailure)
                            {
                                storage.HealthStatus = "Critical";
                                storage.CriticalWarning = "SMART障害予測が検出されました";
                            }

                            break;
                        }

                        // SMART属性データを取得
                        var smartDataQuery = $"SELECT * FROM MSStorageDriver_FailurePredictData WHERE InstanceName LIKE '%{storage.Model}%'";
                        using var dataSearcher = new ManagementObjectSearcher(@"\\.\root\wmi", smartDataQuery);

                        foreach (ManagementObject dataObj in dataSearcher.Get())
                        {
                            var vendorSpecific = dataObj["VendorSpecific"] as byte[];
                            if (vendorSpecific != null && vendorSpecific.Length >= 362)
                            {
                                // SMART属性データをパース（12バイトずつ、30属性分）
                                storage.SmartAttributes = ParseSmartAttributes(vendorSpecific);
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

    /// <summary>
    /// SMART VendorSpecific バイナリデータをパースしてSMART属性リストに変換
    /// </summary>
    /// <param name="vendorSpecific">VendorSpecific バイトデータ</param>
    /// <returns>SMART属性リスト</returns>
    private List<SmartAttribute> ParseSmartAttributes(byte[] vendorSpecific)
    {
        var attributes = new List<SmartAttribute>();

        try
        {
            // VendorSpecific データは2バイトのヘッダー + 30属性（各12バイト）
            // 各属性の構造:
            // [0] ID
            // [1-2] Flags
            // [3] Current Value
            // [4] Worst Value
            // [5] Reserved
            // [6-11] Raw Value (6 bytes, little-endian)

            for (int i = 0; i < 30; i++)
            {
                int offset = 2 + (i * 12); // ヘッダー2バイト + 属性データ

                if (offset + 12 > vendorSpecific.Length)
                    break;

                var id = vendorSpecific[offset];

                // ID が 0 の場合は未使用エントリなのでスキップ
                if (id == 0)
                    continue;

                var currentValue = vendorSpecific[offset + 3];
                var worstValue = vendorSpecific[offset + 4];

                // Raw Value を6バイトのリトルエンディアンで読み取り
                long rawValue = 0;
                for (int j = 0; j < 6; j++)
                {
                    rawValue |= ((long)vendorSpecific[offset + 5 + j]) << (j * 8);
                }

                var attribute = new SmartAttribute
                {
                    Id = id,
                    Name = GetSmartAttributeName(id),
                    CurrentValue = currentValue,
                    WorstValue = worstValue,
                    Threshold = 0, // WMI経由ではしきい値は取得できない
                    RawValue = rawValue
                };

                attributes.Add(attribute);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SMART属性パースエラー: {ex.Message}");
        }

        return attributes;
    }

    /// <summary>
    /// SMART属性IDから属性名を取得
    /// </summary>
    /// <param name="id">属性ID</param>
    /// <returns>属性名</returns>
    private string GetSmartAttributeName(int id)
    {
        return id switch
        {
            0x01 => "Read Error Rate",
            0x02 => "Throughput Performance",
            0x03 => "Spin-Up Time",
            0x04 => "Start/Stop Count",
            0x05 => "Reallocated Sectors Count",
            0x06 => "Read Channel Margin",
            0x07 => "Seek Error Rate",
            0x08 => "Seek Time Performance",
            0x09 => "Power-On Hours",
            0x0A => "Spin Retry Count",
            0x0B => "Recalibration Retries",
            0x0C => "Power Cycle Count",
            0x0D => "Soft Read Error Rate",
            0xAA => "Available Reserved Space",
            0xAB => "SSD Program Fail Count",
            0xAC => "SSD Erase Fail Count",
            0xAD => "SSD Wear Leveling Count",
            0xAE => "Unexpected Power Loss Count",
            0xAF => "Power Loss Protection Failure",
            0xB0 => "Erase Fail Count",
            0xB1 => "Wear Range Delta",
            0xB3 => "Used Reserved Block Count",
            0xB4 => "Unused Reserved Block Count",
            0xB5 => "Program Fail Count Total",
            0xB6 => "Erase Fail Count",
            0xB7 => "SATA Downshift Error Count",
            0xB8 => "End-to-End Error",
            0xB9 => "Head Stability",
            0xBA => "Induced Op-Vibration Detection",
            0xBB => "Reported Uncorrectable Errors",
            0xBC => "Command Timeout",
            0xBD => "High Fly Writes",
            0xBE => "Airflow Temperature",
            0xBF => "G-Sense Error Rate",
            0xC0 => "Power-Off Retract Count",
            0xC1 => "Load/Unload Cycle Count",
            0xC2 => "Temperature",
            0xC3 => "Hardware ECC Recovered",
            0xC4 => "Reallocation Event Count",
            0xC5 => "Current Pending Sector Count",
            0xC6 => "Uncorrectable Sector Count",
            0xC7 => "UltraDMA CRC Error Count",
            0xC8 => "Multi-Zone Error Rate",
            0xC9 => "Soft Read Error Rate",
            0xCA => "Data Address Mark Errors",
            0xCB => "Run Out Cancel",
            0xCC => "Soft ECC Correction",
            0xCD => "Thermal Asperity Rate",
            0xCE => "Flying Height",
            0xCF => "Spin High Current",
            0xD0 => "Spin Buzz",
            0xD1 => "Offline Seek Performance",
            0xD3 => "Vibration During Write",
            0xD4 => "Shock During Write",
            0xDC => "Disk Shift",
            0xDD => "G-Sense Error Rate",
            0xDE => "Loaded Hours",
            0xDF => "Load/Unload Retry Count",
            0xE0 => "Load Friction",
            0xE1 => "Load/Unload Cycle Count",
            0xE2 => "Load-in Time",
            0xE3 => "Torque Amplification Count",
            0xE4 => "Power-Off Retract Cycle",
            0xE6 => "GMR Head Amplitude",
            0xE7 => "Temperature",
            0xE8 => "Endurance Remaining",
            0xE9 => "Power-On Hours",
            0xEA => "Average Erase Count",
            0xEB => "Good Block Count",
            0xF0 => "Head Flying Hours",
            0xF1 => "Total LBAs Written",
            0xF2 => "Total LBAs Read",
            0xF3 => "Total LBAs Written Expanded",
            0xF4 => "Total LBAs Read Expanded",
            0xF9 => "NAND Writes (1GiB)",
            0xFA => "Read Error Retry Rate",
            0xFB => "Minimum Spares Remaining",
            0xFC => "Newly Added Bad Flash Block",
            0xFE => "Free Fall Protection",
            _ => $"Unknown Attribute {id:X2}h"
        };
    }

    /// <summary>
    /// Windowsライセンス認証情報を取得
    /// </summary>
    private WindowsLicenseInfo GetWindowsLicenseInfo()
    {
        var licenseInfo = new WindowsLicenseInfo();

        try
        {
            // SoftwareLicensingProduct からWindowsライセンス情報を取得
            // ApplicationID = "55c92734-d682-4d71-983e-d6ec3f16059f" はWindowsを示す
            var query = "SELECT * FROM SoftwareLicensingProduct WHERE ApplicationID = '55c92734-d682-4d71-983e-d6ec3f16059f' AND PartialProductKey IS NOT NULL";
            using var searcher = new ManagementObjectSearcher(query);

            foreach (ManagementObject obj in searcher.Get())
            {
                // ライセンスステータスを取得
                var licenseStatus = Convert.ToInt32(obj["LicenseStatus"] ?? 0);
                licenseInfo.LicenseStatus = GetLicenseStatusString(licenseStatus);
                licenseInfo.IsActivated = licenseStatus == 1; // 1 = Licensed (認証済み)

                // エディション名
                var name = obj["Name"]?.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    licenseInfo.Edition = name;
                }

                // プロダクトキーの一部（下5桁）
                var partialProductKey = obj["PartialProductKey"]?.ToString();
                if (!string.IsNullOrEmpty(partialProductKey))
                {
                    licenseInfo.PartialProductKey = partialProductKey;
                }

                // ライセンスファミリー
                var licenseFamily = obj["LicenseFamily"]?.ToString();
                if (!string.IsNullOrEmpty(licenseFamily))
                {
                    licenseInfo.LicenseFamily = licenseFamily;
                }

                // 猶予期間の残り時間（分単位）
                var gracePeriodRemaining = obj["GracePeriodRemaining"];
                if (gracePeriodRemaining != null)
                {
                    var minutes = Convert.ToInt32(gracePeriodRemaining);
                    if (minutes > 0)
                    {
                        licenseInfo.GracePeriodRemainingMinutes = minutes;
                    }
                }

                // 最初に見つかったアクティブなライセンスのみ使用
                break;
            }

            // ライセンス情報が取得できなかった場合
            if (string.IsNullOrEmpty(licenseInfo.Edition))
            {
                // Win32_OperatingSystem から基本的なOS情報を取得
                using var osSearcher = new ManagementObjectSearcher("SELECT Caption, Version FROM Win32_OperatingSystem");
                foreach (ManagementObject os in osSearcher.Get())
                {
                    licenseInfo.Edition = os["Caption"]?.ToString() ?? "不明";
                    licenseInfo.Note = "ライセンス詳細情報の取得に失敗しました。基本情報のみ表示しています。";
                    break;
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            licenseInfo.Error = "アクセス拒否";
            licenseInfo.Note = "ライセンス情報の取得には管理者権限が必要な場合があります。";
        }
        catch (Exception ex)
        {
            licenseInfo.Error = $"取得失敗: {ex.Message}";
            licenseInfo.Note = "Windowsライセンス情報の取得に失敗しました。";
        }

        return licenseInfo;
    }

    /// <summary>
    /// ライセンスステータスコードから文字列を取得
    /// </summary>
    /// <param name="status">ライセンスステータスコード</param>
    /// <returns>ステータス文字列</returns>
    private string GetLicenseStatusString(int status)
    {
        return status switch
        {
            0 => "未認証 (Unlicensed)",
            1 => "認証済み (Licensed)",
            2 => "OOB猶予期間 (OOB Grace)",
            3 => "OOT猶予期間 (OOT Grace)",
            4 => "非正規猶予期間 (Non-Genuine Grace)",
            5 => "通知モード (Notification)",
            6 => "延長猶予期間 (Extended Grace)",
            _ => $"不明 ({status})"
        };
    }
}
