# RukiCheck ポータブル検品ツール - アーキテクチャ設計書

## 1. 全体アーキテクチャ

### 1.1 レイヤー構成

```
┌─────────────────────────────────────────┐
│       Presentation Layer (WPF)          │
│  - MainWindow                           │
│  - StartupView, InspectionMenuView      │
│  - ViewModels (MVVM)                    │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│       Application Layer                 │
│  - InspectionOrchestrator               │
│  - ReportGenerator                      │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│       Domain Layer                      │
│  - InspectionReport (Model)             │
│  - InspectionServices (各種検査)         │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│       Infrastructure Layer              │
│  - FileSystemService                    │
│  - JsonSerializer, HtmlGenerator        │
└─────────────────────────────────────────┘
```

### 1.2 ポータブル要件への対応

| 要件 | 対応方針 |
|------|----------|
| インストール不要 | 単一EXE + 依存DLL埋め込み（ILMerge/PublishSingleFile） |
| レジストリ書き込み禁止 | アプリケーション設定はメモリのみ、永続化不要 |
| 管理者権限不要 | `requestedExecutionLevel="asInvoker"` 指定 |
| USB保存 | ユーザー選択フォルダへの書き込み権限のみ使用 |
| TEMP使用 | 録音・キャプチャの一時ファイルのみ、終了時削除 |

---

## 2. クラス設計

### 2.1 名前空間構成

```
RukiCheck
├── Models                  # データモデル
│   ├── InspectionReport.cs
│   ├── InspectionMeta.cs
│   ├── StorageResult.cs
│   ├── KeyboardResult.cs
│   ├── AudioResult.cs
│   ├── CameraResult.cs
│   └── CpuResult.cs
├── Services                # 検査ロジック
│   ├── IInspectionService.cs
│   ├── StorageInspectionService.cs
│   ├── KeyboardInspectionService.cs
│   ├── MicrophoneInspectionService.cs
│   ├── SpeakerInspectionService.cs
│   ├── CameraInspectionService.cs
│   └── CpuInspectionService.cs
├── Infrastructure          # 基盤機能
│   ├── FileSystemService.cs
│   ├── JsonReportWriter.cs
│   └── HtmlReportGenerator.cs
├── Application             # アプリケーションロジック
│   ├── InspectionOrchestrator.cs
│   └── InspectionSession.cs
├── ViewModels              # MVVM ViewModels
│   ├── MainViewModel.cs
│   ├── StartupViewModel.cs
│   └── InspectionMenuViewModel.cs
└── Views                   # WPF Views
    ├── MainWindow.xaml
    ├── StartupView.xaml
    ├── InspectionMenuView.xaml
    ├── KeyboardTestView.xaml
    └── ResultView.xaml
```

---

## 3. 主要クラス詳細

### 3.1 Models / InspectionReport.cs

```csharp
public class InspectionReport
{
    public InspectionMeta Meta { get; set; }
    public StorageResult Storage { get; set; }
    public KeyboardResult Keyboard { get; set; }
    public AudioResult Microphone { get; set; }
    public AudioResult Speaker { get; set; }
    public CameraResult Camera { get; set; }
    public CpuResult Cpu { get; set; }
}

public class InspectionMeta
{
    public string ManagementId { get; set; }
    public string InspectionDate { get; set; }    // YYYY-MM-DD
    public string InspectionTime { get; set; }    // HH:mm:ss
    public string Mode { get; set; } = "standard";
    public string ToolVersion { get; set; } = "v0.1.0";
}

public class StorageResult
{
    public string Drive { get; set; }
    public double TotalGb { get; set; }
    public double FreeGb { get; set; }
    public double FreePercent { get; set; }
    public string Status { get; set; }  // ok / warn / danger
    public string Recommendation { get; set; }
}

public class KeyboardResult
{
    public string Layout { get; set; }  // JIS / US
    public int TotalKeys { get; set; }
    public int PressedKeys { get; set; }
    public List<string> MissingKeys { get; set; }
    public string Result { get; set; }  // pass / warn
}

public class AudioResult
{
    public bool Recorded { get; set; }
    public string File { get; set; }
    public bool UserConfirmed { get; set; }
    public bool Left { get; set; }  // Speaker用
    public bool Right { get; set; } // Speaker用
}

public class CameraResult
{
    public bool Captured { get; set; }
    public string File { get; set; }
    public string Note { get; set; }
}

public class CpuResult
{
    public string StressTest { get; set; }  // completed / failed
    public int DurationSec { get; set; }
    public bool Abnormal { get; set; }
    public double? Temperature { get; set; }  // nullable（取得できない場合あり）
}
```

### 3.2 Services / IInspectionService.cs

```csharp
public interface IInspectionService<T>
{
    Task<T> ExecuteAsync(string attachmentPath);
}
```

### 3.3 Infrastructure / FileSystemService.cs

```csharp
public class FileSystemService
{
    public string CreateInspectionFolder(string basePath, string managementId, DateTime date)
    {
        // {USB}:\RukiCheck\{管理番号}_{YYYYMMDD}\
        var folderName = $"{managementId}_{date:yyyyMMdd}";
        var fullPath = Path.Combine(basePath, "RukiCheck", folderName);

        Directory.CreateDirectory(fullPath);
        Directory.CreateDirectory(Path.Combine(fullPath, "attachments"));

        return fullPath;
    }

    public bool HasWritePermission(string path)
    {
        try
        {
            var testFile = Path.Combine(path, $".test_{Guid.NewGuid()}");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

### 3.4 Application / InspectionOrchestrator.cs

```csharp
public class InspectionOrchestrator
{
    private readonly FileSystemService _fileSystem;
    private readonly JsonReportWriter _jsonWriter;
    private readonly HtmlReportGenerator _htmlGenerator;

    public InspectionSession CreateSession(string managementId, string basePath)
    {
        var outputPath = _fileSystem.CreateInspectionFolder(basePath, managementId, DateTime.Now);

        return new InspectionSession
        {
            ManagementId = managementId,
            OutputPath = outputPath,
            Report = new InspectionReport
            {
                Meta = new InspectionMeta
                {
                    ManagementId = managementId,
                    InspectionDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    InspectionTime = DateTime.Now.ToString("HH:mm:ss")
                }
            }
        };
    }

    public async Task SaveReportAsync(InspectionSession session)
    {
        // JSON保存
        var jsonPath = Path.Combine(session.OutputPath, "report.json");
        await _jsonWriter.WriteAsync(jsonPath, session.Report);

        // HTML生成
        var htmlPath = Path.Combine(session.OutputPath, "report.html");
        await _htmlGenerator.GenerateAsync(htmlPath, session.Report);
    }
}
```

---

## 4. UI設計（WPF）

### 4.1 画面遷移フロー

```
StartupView
  ↓ (管理番号入力 + 保存先選択)
InspectionMenuView
  ↓ (各検査項目選択)
  ├→ StorageTestView (自動実行)
  ├→ KeyboardTestView (キー入力UI)
  ├→ MicrophoneTestView (録音・再生)
  ├→ SpeakerTestView (L/R再生)
  ├→ CameraTestView (プレビュー・撮影)
  └→ CpuTestView (進捗表示)
  ↓
ResultView (完了・レポート確認)
```

### 4.2 MVVMパターン適用

- **View**: XAMLでUI定義
- **ViewModel**: `INotifyPropertyChanged` 実装、コマンドバインディング
- **Model**: ビジネスロジック（Services）

### 4.3 依存性注入（DI）

```csharp
// App.xaml.cs
public partial class App : Application
{
    private ServiceProvider _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();

        // Services
        services.AddSingleton<FileSystemService>();
        services.AddSingleton<JsonReportWriter>();
        services.AddSingleton<HtmlReportGenerator>();
        services.AddSingleton<InspectionOrchestrator>();

        services.AddTransient<IInspectionService<StorageResult>, StorageInspectionService>();
        services.AddTransient<IInspectionService<KeyboardResult>, KeyboardInspectionService>();
        // ... 他のサービス

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<StartupViewModel>();
        services.AddTransient<InspectionMenuViewModel>();

        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = new MainWindow
        {
            DataContext = _serviceProvider.GetService<MainViewModel>()
        };
        mainWindow.Show();
    }
}
```

---

## 5. ポータブルEXE生成方法

### 5.1 .csproj 設定

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <SelfContained>true</SelfContained>
    <PublishSingleFile>true</PublishSingleFile>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
    <ApplicationManifest>app.manifest</ApplicationManifest>
  </PropertyGroup>
</Project>
```

### 5.2 app.manifest（管理者権限無効化）

```xml
<requestedExecutionLevel level="asInvoker" uiAccess="false" />
```

### 5.3 ビルドコマンド

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

---

## 6. セキュリティと権限

### 6.1 標準ユーザーで動作する実装

| 機能 | 実装方法 |
|------|----------|
| ドライブ情報取得 | `DriveInfo` クラス（読み取り専用） |
| キーボード入力 | WPF `KeyDown` イベント |
| マイク録音 | `NAudio` ライブラリ（ユーザー権限で可能） |
| カメラアクセス | `OpenCvSharp` または `MediaCapture`（UWP API） |
| CPU負荷テスト | `Parallel.For` で計算負荷 |

### 6.2 権限エラーハンドリング

```csharp
try
{
    // カメラアクセス試行
    var result = await cameraService.CaptureAsync();
}
catch (UnauthorizedAccessException)
{
    result.Note = "カメラはWindowsプライバシー設定により取得不可";
    result.Captured = false;
}
```

---

## 7. 外部ライブラリ

| ライブラリ | 用途 | ライセンス |
|-----------|------|-----------|
| `System.Text.Json` | JSON出力 | MIT（.NET標準） |
| `NAudio` | 音声録音・再生 | MIT |
| `OpenCvSharp4` | カメラキャプチャ | Apache 2.0 |
| `Microsoft.Extensions.DependencyInjection` | DI | MIT |

---

## 8. 実装上の重要注意点

### 8.1 TEMP使用ルール

- 録音・撮影は一時的に `Path.GetTempPath()` 使用OK
- 検査完了後に `attachments\` へ移動
- アプリ終了時に TEMP クリーンアップ

### 8.2 非同期処理

- UI凍結防止のため、すべての検査処理は `async/await`
- 長時間処理は `Progress<T>` でプログレス表示

### 8.3 エラーリカバリ

- 1つの検査が失敗しても、他の検査は継続
- 失敗した項目は JSON に `"error": "取得失敗理由"` を記録

### 8.4 文字エンコーディング

- JSON/HTML は UTF-8（BOM付き）で保存
- ファイル名は Windows互換文字のみ

---

## 9. テスト戦略

### 9.1 単体テスト

- 各 `InspectionService` の独立テスト
- モック使用（`Moq` ライブラリ）

### 9.2 統合テスト

- 実機5台での動作確認
  - 標準ユーザーアカウントで実行
  - USB保存の確認
  - report.json / report.html の妥当性チェック

---

## 10. 完成条件チェックリスト

- [ ] 単体EXEが生成できる（PublishSingleFile）
- [ ] 管理者権限なしで起動できる
- [ ] USBへの保存フォルダが正しく生成される
- [ ] report.json が仕様通りの形式で出力される
- [ ] report.html が人間に読みやすい形式で出力される
- [ ] 全検査項目が標準ユーザー権限で実行できる
- [ ] エラー発生時も他の検査は継続できる
- [ ] アプリ終了後、PCに永続的な変更が残らない
- [ ] 実機5台以上でテストして完走する

---

**設計承認後、実装フェーズへ移行**
