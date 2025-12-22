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
}
