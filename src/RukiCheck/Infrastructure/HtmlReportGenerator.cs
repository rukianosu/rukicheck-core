using System.Text;
using RukiCheck.Models;

namespace RukiCheck.Infrastructure;

/// <summary>
/// HTML レポート生成サービス
/// </summary>
public class HtmlReportGenerator
{
    /// <summary>
    /// レポートをHTML形式で生成・保存
    /// </summary>
    public async Task GenerateAsync(string filePath, InspectionReport report)
    {
        var html = BuildHtml(report);

        // UTF-8 BOM付きで保存
        var utf8WithBom = new UTF8Encoding(true);
        await File.WriteAllTextAsync(filePath, html, utf8WithBom);
    }

    private string BuildHtml(InspectionReport report)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"ja\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"    <title>検品レポート - {report.Meta.ManagementId}</title>");
        sb.AppendLine("    <style>");
        sb.AppendLine(GetCss());
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // ヘッダー
        sb.AppendLine("    <div class=\"header\">");
        sb.AppendLine("        <h1>🔍 PC検品レポート</h1>");
        sb.AppendLine($"        <p class=\"subtitle\">管理番号: {report.Meta.ManagementId}</p>");
        sb.AppendLine($"        <p class=\"subtitle\">検品日時: {report.Meta.InspectionDate} {report.Meta.InspectionTime}</p>");
        sb.AppendLine("    </div>");

        sb.AppendLine("    <div class=\"container\">");

        // ストレージ
        if (report.Storage != null)
        {
            sb.AppendLine(BuildStorageSection(report.Storage));
        }

        // キーボード
        if (report.Keyboard != null)
        {
            sb.AppendLine(BuildKeyboardSection(report.Keyboard));
        }

        // マイク
        if (report.Microphone != null)
        {
            sb.AppendLine(BuildMicrophoneSection(report.Microphone));
        }

        // スピーカー
        if (report.Speaker != null)
        {
            sb.AppendLine(BuildSpeakerSection(report.Speaker));
        }

        // カメラ
        if (report.Camera != null)
        {
            sb.AppendLine(BuildCameraSection(report.Camera));
        }

        // トラックパッド
        if (report.Trackpad != null)
        {
            sb.AppendLine(BuildTrackpadSection(report.Trackpad));
        }

        // CPU
        if (report.Cpu != null)
        {
            sb.AppendLine(BuildCpuSection(report.Cpu));
        }

        sb.AppendLine("    </div>");

        // フッター
        sb.AppendLine("    <div class=\"footer\">");
        sb.AppendLine($"        <p>RukiCheck {report.Meta.ToolVersion} - 生成日時: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine("    </div>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private string BuildStorageSection(StorageResult storage)
    {
        var statusClass = storage.Status switch
        {
            "ok" => "status-ok",
            "warn" => "status-warn",
            "danger" => "status-danger",
            _ => "status-unknown"
        };

        var statusLabel = storage.Status switch
        {
            "ok" => "✅ 正常",
            "warn" => "⚠️ 注意",
            "danger" => "🚨 危険",
            _ => "❓ 不明"
        };

        var sb = new StringBuilder();
        sb.AppendLine("        <div class=\"section\">");
        sb.AppendLine("            <h2>💾 ストレージ空き容量</h2>");
        sb.AppendLine($"            <div class=\"{statusClass}\">{statusLabel}</div>");
        sb.AppendLine("            <table>");
        sb.AppendLine($"                <tr><th>ドライブ</th><td>{storage.Drive}</td></tr>");
        sb.AppendLine($"                <tr><th>総容量</th><td>{storage.TotalGb:F1} GB</td></tr>");
        sb.AppendLine($"                <tr><th>空き容量</th><td>{storage.FreeGb:F1} GB</td></tr>");
        sb.AppendLine($"                <tr><th>空き率</th><td>{storage.FreePercent:F1}%</td></tr>");

        if (!string.IsNullOrEmpty(storage.Recommendation))
        {
            sb.AppendLine($"                <tr><th>推奨</th><td class=\"recommendation\">{storage.Recommendation}</td></tr>");
        }

        if (!string.IsNullOrEmpty(storage.Error))
        {
            sb.AppendLine($"                <tr><th>エラー</th><td class=\"error\">{storage.Error}</td></tr>");
        }

        sb.AppendLine("            </table>");
        sb.AppendLine("        </div>");

        return sb.ToString();
    }

    private string BuildKeyboardSection(KeyboardResult keyboard)
    {
        var statusClass = keyboard.Result switch
        {
            "pass" => "status-ok",
            "warn" => "status-warn",
            _ => "status-danger"
        };

        var statusLabel = keyboard.Result switch
        {
            "pass" => "✅ 合格",
            "warn" => "⚠️ 一部未入力",
            _ => "❌ 不合格"
        };

        var sb = new StringBuilder();
        sb.AppendLine("        <div class=\"section\">");
        sb.AppendLine("            <h2>⌨️ キーボード</h2>");
        sb.AppendLine($"            <div class=\"{statusClass}\">{statusLabel}</div>");
        sb.AppendLine("            <table>");
        sb.AppendLine($"                <tr><th>配列</th><td>{keyboard.Layout}</td></tr>");
        sb.AppendLine($"                <tr><th>総キー数</th><td>{keyboard.TotalKeys}</td></tr>");
        sb.AppendLine($"                <tr><th>押下キー数</th><td>{keyboard.PressedKeys}</td></tr>");

        if (keyboard.MissingKeys.Any())
        {
            sb.AppendLine($"                <tr><th>未入力キー</th><td class=\"error\">{string.Join(", ", keyboard.MissingKeys)}</td></tr>");
        }

        if (!string.IsNullOrEmpty(keyboard.Note))
        {
            sb.AppendLine($"                <tr><th>備考</th><td>{keyboard.Note}</td></tr>");
        }

        sb.AppendLine("            </table>");
        sb.AppendLine("        </div>");

        return sb.ToString();
    }

    private string BuildMicrophoneSection(MicrophoneResult mic)
    {
        var statusClass = mic.Recorded && mic.UserConfirmed ? "status-ok" : "status-warn";
        var statusLabel = mic.Recorded && mic.UserConfirmed ? "✅ 正常" : "⚠️ 確認不可または未確認";

        var sb = new StringBuilder();
        sb.AppendLine("        <div class=\"section\">");
        sb.AppendLine("            <h2>🎤 マイク</h2>");
        sb.AppendLine($"            <div class=\"{statusClass}\">{statusLabel}</div>");
        sb.AppendLine("            <table>");
        sb.AppendLine($"                <tr><th>録音</th><td>{(mic.Recorded ? "成功" : "失敗")}</td></tr>");

        if (mic.Recorded && !string.IsNullOrEmpty(mic.File))
        {
            sb.AppendLine($"                <tr><th>音声ファイル</th><td><a href=\"{mic.File}\" target=\"_blank\">📁 {mic.File}</a></td></tr>");
            sb.AppendLine($"                <tr><th>ユーザー確認</th><td>{(mic.UserConfirmed ? "✅ 音が聞こえた" : "❌ 音が聞こえなかった")}</td></tr>");
        }

        if (!string.IsNullOrEmpty(mic.DeviceName))
        {
            sb.AppendLine($"                <tr><th>デバイス</th><td>{mic.DeviceName}</td></tr>");
        }

        if (!string.IsNullOrEmpty(mic.Error))
        {
            sb.AppendLine($"                <tr><th>エラー</th><td class=\"error\">{mic.Error}</td></tr>");
        }

        sb.AppendLine("            </table>");
        sb.AppendLine("        </div>");

        return sb.ToString();
    }

    private string BuildSpeakerSection(SpeakerResult speaker)
    {
        var allOk = speaker.Left && speaker.Right;
        var statusClass = allOk ? "status-ok" : "status-warn";
        var statusLabel = allOk ? "✅ 正常" : "⚠️ 一部不良";

        var sb = new StringBuilder();
        sb.AppendLine("        <div class=\"section\">");
        sb.AppendLine("            <h2>🔊 スピーカー</h2>");
        sb.AppendLine($"            <div class=\"{statusClass}\">{statusLabel}</div>");
        sb.AppendLine("            <table>");
        sb.AppendLine($"                <tr><th>左チャンネル</th><td>{(speaker.Left ? "✅ 正常" : "❌ 不良")}</td></tr>");
        sb.AppendLine($"                <tr><th>右チャンネル</th><td>{(speaker.Right ? "✅ 正常" : "❌ 不良")}</td></tr>");

        if (!string.IsNullOrEmpty(speaker.DeviceName))
        {
            sb.AppendLine($"                <tr><th>デバイス</th><td>{speaker.DeviceName}</td></tr>");
        }

        sb.AppendLine("            </table>");
        sb.AppendLine("        </div>");

        return sb.ToString();
    }

    private string BuildCameraSection(CameraResult camera)
    {
        var statusClass = camera.Captured ? "status-ok" : "status-warn";
        var statusLabel = camera.Captured ? "✅ 正常" : "⚠️ キャプチャ失敗";

        var sb = new StringBuilder();
        sb.AppendLine("        <div class=\"section\">");
        sb.AppendLine("            <h2>📷 カメラ</h2>");
        sb.AppendLine($"            <div class=\"{statusClass}\">{statusLabel}</div>");
        sb.AppendLine("            <table>");
        sb.AppendLine($"                <tr><th>キャプチャ</th><td>{(camera.Captured ? "成功" : "失敗")}</td></tr>");

        if (camera.Captured && !string.IsNullOrEmpty(camera.File))
        {
            sb.AppendLine($"                <tr><th>画像ファイル</th><td><a href=\"{camera.File}\" target=\"_blank\">📁 {camera.File}</a></td></tr>");
        }

        if (!string.IsNullOrEmpty(camera.Note))
        {
            sb.AppendLine($"                <tr><th>備考</th><td>{camera.Note}</td></tr>");
        }

        if (!string.IsNullOrEmpty(camera.Error))
        {
            sb.AppendLine($"                <tr><th>エラー</th><td class=\"error\">{camera.Error}</td></tr>");
        }

        sb.AppendLine("            </table>");
        sb.AppendLine("        </div>");

        return sb.ToString();
    }

    private string BuildTrackpadSection(TrackpadResult trackpad)
    {
        var statusClass = trackpad.Result switch
        {
            "pass" => "status-ok",
            "warn" => "status-warn",
            _ => "status-danger"
        };

        var statusLabel = trackpad.Result switch
        {
            "pass" => "✅ 正常",
            "warn" => "⚠️ 一部不良",
            _ => "❌ 不良"
        };

        var sb = new StringBuilder();
        sb.AppendLine("        <div class=\"section\">");
        sb.AppendLine("            <h2>🖱️ トラックパッド</h2>");
        sb.AppendLine($"            <div class=\"{statusClass}\">{statusLabel}</div>");
        sb.AppendLine("            <table>");
        sb.AppendLine($"                <tr><th>カーソル移動</th><td>{(trackpad.CursorMoved ? "✅ 正常" : "❌ 不良")}</td></tr>");
        sb.AppendLine($"                <tr><th>左クリック</th><td>{(trackpad.LeftClick ? "✅ 正常" : "❌ 不良")}</td></tr>");
        sb.AppendLine($"                <tr><th>右クリック</th><td>{(trackpad.RightClick ? "✅ 正常" : "❌ 不良")}</td></tr>");
        sb.AppendLine($"                <tr><th>スクロール</th><td>{(trackpad.ScrollDetected ? "✅ 正常" : "❌ 不良")}</td></tr>");

        if (!string.IsNullOrEmpty(trackpad.Note))
        {
            sb.AppendLine($"                <tr><th>備考</th><td>{trackpad.Note}</td></tr>");
        }

        if (!string.IsNullOrEmpty(trackpad.Error))
        {
            sb.AppendLine($"                <tr><th>エラー</th><td class=\"error\">{trackpad.Error}</td></tr>");
        }

        sb.AppendLine("            </table>");
        sb.AppendLine("        </div>");

        return sb.ToString();
    }

    private string BuildCpuSection(CpuResult cpu)
    {
        var statusClass = cpu.StressTest == "completed" && !cpu.Abnormal ? "status-ok" : "status-warn";
        var statusLabel = cpu.StressTest == "completed" && !cpu.Abnormal ? "✅ 正常" : "⚠️ 異常または未完了";

        var sb = new StringBuilder();
        sb.AppendLine("        <div class=\"section\">");
        sb.AppendLine("            <h2>🖥️ CPU簡易テスト</h2>");
        sb.AppendLine($"            <div class=\"{statusClass}\">{statusLabel}</div>");
        sb.AppendLine("            <table>");
        sb.AppendLine($"                <tr><th>テスト結果</th><td>{cpu.StressTest}</td></tr>");
        sb.AppendLine($"                <tr><th>実行時間</th><td>{cpu.DurationSec}秒</td></tr>");
        sb.AppendLine($"                <tr><th>異常検出</th><td>{(cpu.Abnormal ? "あり" : "なし")}</td></tr>");

        if (cpu.Temperature.HasValue)
        {
            sb.AppendLine($"                <tr><th>温度</th><td>{cpu.Temperature:F1}°C</td></tr>");
        }

        if (!string.IsNullOrEmpty(cpu.Note))
        {
            sb.AppendLine($"                <tr><th>備考</th><td>{cpu.Note}</td></tr>");
        }

        sb.AppendLine("            </table>");
        sb.AppendLine("        </div>");

        return sb.ToString();
    }

    private string GetCss()
    {
        return @"
        body {
            font-family: 'Segoe UI', Meiryo, sans-serif;
            margin: 0;
            padding: 0;
            background: #f5f5f5;
        }
        .header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            text-align: center;
        }
        .header h1 {
            margin: 0;
            font-size: 2.5em;
        }
        .subtitle {
            margin: 5px 0;
            opacity: 0.9;
        }
        .container {
            max-width: 900px;
            margin: 30px auto;
            padding: 0 20px;
        }
        .section {
            background: white;
            border-radius: 10px;
            padding: 25px;
            margin-bottom: 25px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }
        .section h2 {
            margin-top: 0;
            color: #333;
            border-bottom: 2px solid #667eea;
            padding-bottom: 10px;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 15px;
        }
        th, td {
            padding: 12px;
            text-align: left;
            border-bottom: 1px solid #eee;
        }
        th {
            background: #f9f9f9;
            font-weight: bold;
            width: 30%;
        }
        .status-ok {
            background: #d4edda;
            color: #155724;
            padding: 10px;
            border-radius: 5px;
            font-weight: bold;
            text-align: center;
            margin-bottom: 15px;
        }
        .status-warn {
            background: #fff3cd;
            color: #856404;
            padding: 10px;
            border-radius: 5px;
            font-weight: bold;
            text-align: center;
            margin-bottom: 15px;
        }
        .status-danger {
            background: #f8d7da;
            color: #721c24;
            padding: 10px;
            border-radius: 5px;
            font-weight: bold;
            text-align: center;
            margin-bottom: 15px;
        }
        .status-unknown {
            background: #e2e3e5;
            color: #383d41;
            padding: 10px;
            border-radius: 5px;
            font-weight: bold;
            text-align: center;
            margin-bottom: 15px;
        }
        .error {
            color: #dc3545;
            font-weight: bold;
        }
        .recommendation {
            color: #007bff;
            font-weight: bold;
        }
        a {
            color: #667eea;
            text-decoration: none;
        }
        a:hover {
            text-decoration: underline;
        }
        .footer {
            text-align: center;
            padding: 20px;
            color: #666;
            font-size: 0.9em;
        }
        ";
    }
}
