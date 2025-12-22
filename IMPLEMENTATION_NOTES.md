# RukiCheck 実装上の注意点

## 🎯 設計思想

「検品表を書く」のではなく、「検品結果が自動で残る」こと。

---

## 1. ポータブルEXE要件の実現方法

### 1.1 単一EXE化

**.csproj 設定**:
```xml
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>true</SelfContained>
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
```

- すべての依存DLL（NAudio, OpenCvSharp等）をEXEに埋め込み
- ネイティブライブラリ（opencv_world等）も自動展開
- 初回実行時に一時フォルダへ展開されるが、終了後は削除される

### 1.2 管理者権限の無効化

**app.manifest**:
```xml
<requestedExecutionLevel level="asInvoker" uiAccess="false" />
```

- `asInvoker` = 実行ユーザーと同じ権限で動作
- 管理者権限昇格ダイアログを表示しない

---

## 2. 標準ユーザー権限での動作

### 2.1 権限が必要ない処理

| 処理 | API | 備考 |
|------|-----|------|
| ドライブ情報取得 | `DriveInfo.GetDrives()` | 読み取り専用API |
| キーボード入力 | WPF `KeyDown` イベント | UIイベント |
| マイク録音 | `NAudio.WaveInEvent` | ユーザープロセス権限で可能 |
| スピーカー再生 | `NAudio.WaveOutEvent` | ユーザープロセス権限で可能 |
| カメラアクセス | `OpenCvSharp.VideoCapture` | Windowsプライバシー設定に依存 |
| CPU負荷 | `Parallel.For` | 通常の計算処理 |

### 2.2 権限エラーのハンドリング

```csharp
try
{
    var result = await cameraService.CaptureAsync();
}
catch (UnauthorizedAccessException)
{
    result.Note = "カメラはWindowsプライバシー設定により取得不可";
    result.Captured = false;
}
```

- 全サービスで `UnauthorizedAccessException` をキャッチ
- エラー理由をJSON/HTMLに記録
- **検査は継続する**（1つの失敗が全体を止めない）

---

## 3. ファイル保存戦略

### 3.1 TEMP使用ルール

✅ **OK（実行中のみ）**:
```csharp
var tempFile = Path.Combine(Path.GetTempPath(), $"rukicheck_{Guid.NewGuid()}.wav");
File.WriteAllBytes(tempFile, data);

// 検査完了後に移動
File.Move(tempFile, finalPath);
```

❌ **NG（永続化）**:
```csharp
// TEMPに残したまま終了 → NG
File.WriteAllBytes(tempFile, data);
// アプリ終了
```

### 3.2 フォルダ作成権限チェック

```csharp
public bool HasWritePermission(string path)
{
    try
    {
        var testFile = Path.Combine(path, $".test_{Guid.NewGuid()}.tmp");
        File.WriteAllText(testFile, "test");
        File.Delete(testFile);
        return true;
    }
    catch
    {
        return false;
    }
}
```

- USBドライブが読み取り専用でないか事前確認
- 書き込み失敗時はエラーメッセージ表示

---

## 4. 非同期処理とUI凍結防止

### 4.1 長時間処理の非同期化

❌ **NG（UI凍結）**:
```csharp
// ボタンクリックで同期実行
private void OnButtonClick()
{
    Thread.Sleep(60000); // 60秒間UI凍結
}
```

✅ **OK（非同期）**:
```csharp
// AsyncRelayCommand使用
public AsyncRelayCommand RunCpuTestCommand { get; }

private async Task RunCpuTestAsync()
{
    await Task.Run(() => CpuStressTest());
}
```

### 4.2 プログレス表示

```csharp
StatusMessage = "CPU負荷テスト実行中（60秒）...";

var progress = new Progress<int>(percent =>
{
    StatusMessage = $"CPU負荷テスト実行中... {percent}%";
});

await _cpuService.ExecuteAsync(progress);
```

---

## 5. JSON/HTML生成の注意点

### 5.1 JSON エンコーディング

```csharp
var options = new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) // 日本語エスケープ防止
};

// UTF-8 BOM付きで保存
var utf8WithBom = new UTF8Encoding(true);
await File.WriteAllTextAsync(path, json, utf8WithBom);
```

- **Windows互換性のためBOM付き**
- 日本語をエスケープしない（`\u3042` ではなく `あ`）

### 5.2 HTML相対パス

```html
<a href="attachments/mic_test.wav">録音ファイル</a>
```

- 絶対パスではなく相対パス使用
- フォルダを移動しても動作する

---

## 6. 外部ライブラリの選定基準

### 6.1 採用ライブラリ

| ライブラリ | 理由 | 代替候補 |
|-----------|------|---------|
| NAudio | MIT、Windows標準、実績多数 | CSCore（複雑） |
| OpenCvSharp4 | Apache 2.0、カメラ制御が容易 | AForge.NET（古い） |
| System.Text.Json | .NET標準、追加依存なし | Newtonsoft.Json（外部依存） |

### 6.2 不採用ライブラリ

❌ **LibreHardwareMonitor** - CPU温度取得に管理者権限必要
❌ **CrystalDiskInfo** - SMART情報取得に管理者権限必要
❌ **Windows.Media.Capture** - UWP API、WPFから使いにくい

---

## 7. キーボード検査の実装詳細

### 7.1 期待キーセット

```csharp
// JIS配列109キー（標準）
var expectedKeys = new HashSet<string>
{
    "Escape", "F1", "F2", ..., "F12",
    "D1", "D2", ..., "D0",
    "A", "B", ..., "Z",
    "NumPad0", ..., "NumPad9",
    // ...
};
```

### 7.2 Fnキーの扱い

- **仕様として対象外**
- WPF KeyDownイベントで検出不可
- レポートに明記: "Fnキーは対象外"

### 7.3 キー名マッピング

WPFの `Key` Enumと表示名のマッピング:

| WPF Key | 表示名 | 備考 |
|---------|--------|------|
| `D1` | `1` | 数字行 |
| `OemMinus` | `-` | JIS配列 |
| `Oem1` | `:` | JIS配列 |
| `NumPad0` | `テンキー0` | テンキー |

---

## 8. エラーリカバリ戦略

### 8.1 部分的失敗の許容

```csharp
// ストレージ検査が失敗してもキーボード検査は実行する
try
{
    var storageResult = await _storageService.ExecuteAsync();
    session.Report.Storage = storageResult;
}
catch (Exception ex)
{
    session.Report.Storage = new StorageResult
    {
        Error = ex.Message,
        Status = "error"
    };
}

// 次の検査へ続行
```

### 8.2 ログ出力（v0.2以降）

現在はメッセージボックスで通知、将来的には:
```csharp
File.AppendAllText("debug.log", $"[ERROR] {ex}\n");
```

---

## 9. テスト戦略

### 9.1 単体テスト

```bash
dotnet test
```

- 各サービスクラスのモックテスト
- ファイル生成・削除のテスト

### 9.2 統合テスト（実機）

**必須テスト環境**:
- Windows 10 標準ユーザー × 1台
- Windows 11 標準ユーザー × 1台
- USB3.0ドライブ × 1個
- カメラ/マイク付きPC × 1台
- 最小スペックPC（Celeron等） × 1台

**テスト項目**:
1. [ ] 標準ユーザーで起動できる
2. [ ] USBへの保存が成功する
3. [ ] report.json が正しく生成される
4. [ ] report.html がブラウザで正しく表示される
5. [ ] 録音ファイルが再生できる
6. [ ] 撮影画像が表示できる
7. [ ] CPUテストが完走する
8. [ ] アプリ終了後、TEMPが空になる
9. [ ] レジストリに痕跡がない
10. [ ] USBを抜いてもHTMLが開ける

---

## 10. ビルド最適化

### 10.1 リリースビルドオプション

```bash
dotnet publish -c Release -r win-x64 \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:EnableCompressionInSingleFile=true \
  /p:DebugType=None \
  /p:DebugSymbols=false
```

- 圧縮有効化でEXEサイズ削減
- デバッグシンボル削除で軽量化

### 10.2 トリミング（注意）

```xml
<PublishTrimmed>true</PublishTrimmed>
```

⚠️ **WPFアプリではトリミング非推奨**
→ リフレクション使用箇所で実行時エラーの可能性

---

## 11. 今後の拡張計画（v0.2以降）

### v0.2
- [ ] キーボード検査UI完全実装
- [ ] マイク/スピーカー/カメラ検査UI完全実装
- [ ] プログレスバー表示
- [ ] ログファイル出力

### v0.3
- [ ] 複数PCの一括検品（CSVエクスポート）
- [ ] QRコード生成（管理番号埋め込み）
- [ ] 英語対応

### v1.0
- [ ] 実機100台以上でテスト完走
- [ ] エンタープライズ対応（AD環境テスト）
- [ ] デジタル署名

---

## 12. トラブルシューティング

### Q1. カメラが開けない

**原因**: Windowsプライバシー設定でカメラがブロックされている

**対処**:
```
設定 > プライバシーとセキュリティ > カメラ
→ 「デスクトップアプリがカメラにアクセスできるようにする」をON
```

### Q2. マイクが録音できない

**原因**: マイクがミュートまたは無効化されている

**対処**:
```
設定 > システム > サウンド > 入力
→ 既定のマイクが正しく選択されているか確認
```

### Q3. USB保存時に"権限エラー"

**原因**: USBドライブがFAT32でファイルサイズ制限

**対処**:
- exFATまたはNTFSでフォーマット
- または別のUSBドライブを使用

### Q4. CPUテストが途中で止まる

**原因**: 低スペックPC・熱暴走

**対処**:
- 60秒→30秒に短縮（設定変更）
- ファンの動作確認

---

## 13. コード品質チェックリスト

実装時に確認すること:

- [ ] すべての `async` メソッドに `await` がある
- [ ] すべての `IDisposable` を `using` で囲む
- [ ] すべての例外を `try-catch` でハンドリング
- [ ] UIスレッドで重い処理をしない
- [ ] ファイルパスは `Path.Combine` を使う
- [ ] 文字列比較は `StringComparison.OrdinalIgnoreCase` を使う
- [ ] `null` チェックを忘れない（`?.` 演算子活用）

---

**実装ガイドはここまで。実装頑張ってください！**
