# RukiCheck - Windows ポータブル検品ツール

![Version](https://img.shields.io/badge/version-0.1.0-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![License](https://img.shields.io/badge/license-MIT-green)

## 📋 概要

RukiCheckは、**インストール不要**で動作するWindowsポータブル検品ツールです。
標準ユーザー権限（管理者権限不要）で実行でき、USB等に保存可能な検品結果を出力します。

### 主な特徴

✅ **インストール不要** - 単体EXEファイルのみで動作
✅ **管理者権限不要** - 標準ユーザー／ゲストアカウントでも実行可能
✅ **ポータブル** - USBメモリから実行してUSBに結果を保存
✅ **証拠付きレポート** - JSON + HTML + 音声/画像ファイル
✅ **PC無改変** - レジストリ・サービス追加なし、実行終了後は痕跡なし

---

## 🎯 検品項目（v0.1）

| 項目 | 内容 | 自動判定 |
|------|------|---------|
| 💾 **ストレージ** | Cドライブ空き容量チェック | ✅ |
| ⌨️ **キーボード** | 全キー入力確認（JIS/US） | ✅ |
| 🎤 **マイク** | 5秒録音 + 再生確認 | 👤 ユーザー確認 |
| 🔊 **スピーカー** | 左右チャンネルテスト音 | 👤 ユーザー確認 |
| 📷 **カメラ** | プレビュー + 静止画撮影 | ✅ |
| 🖥️ **CPU** | 60秒負荷テスト（フリーズ検知） | ✅ |

---

## 📦 システム要件

- **OS**: Windows 10 / 11（64bit）
- **権限**: 標準ユーザー（管理者権限不要）
- **ランタイム**: 不要（.NET 8ランタイム内蔵）
- **ディスク**: 実行時100MB程度、出力は数MB

---

## 🚀 使い方

### 1. EXEを実行

```
RukiCheck.exe
```

管理者権限は不要です。ダブルクリックで起動します。

### 2. 初期設定

1. **管理番号**を入力（例: `RT-250903`）
2. **保存先フォルダ**を選択（USBドライブ推奨）
3. **検品開始**ボタンをクリック

→ フォルダが自動生成されます：
```
{保存先}\RukiCheck\RT-250903_20250922\
```

### 3. 検査を実行

各検査ボタンをクリックして順番に実行します。

- ストレージは自動判定
- キーボードは全キーを1回ずつ押下
- マイク/スピーカーは音声再生後にYes/No入力
- カメラはプレビュー後に撮影
- CPUは60秒間の負荷テスト

### 4. レポート保存

すべての検査が完了したら、**レポート保存**ボタンをクリック。

以下のファイルが生成されます：

```
RT-250903_20250922\
├── report.json         # 正データ（プログラム用）
├── report.html         # お客様提出用レポート
└── attachments\
    ├── mic_test.wav    # マイク録音
    └── camera_test.jpg # カメラ撮影画像
```

### 5. USBを抜く

検品完了後、USBメモリを取り外せばPC側には何も残りません。

---

## 📂 出力フォルダ構造

```
{USB}\RukiCheck\
 └─ {管理番号}_{YYYYMMDD}\
     ├─ report.json          ← JSONレポート（正データ）
     ├─ report.html          ← HTMLレポート（お客様用）
     └─ attachments\
         ├─ mic_test.wav     ← 録音ファイル
         └─ camera_test.jpg  ← 撮影画像
```

---

## 🛠️ ビルド方法（開発者向け）

### 前提条件

- **.NET 8 SDK** がインストールされていること
- Windows環境（クロスプラットフォームビルドは未対応）

### ビルド手順

```bash
# リポジトリをクローン
git clone https://github.com/yourusername/rukicheck-core.git
cd rukicheck-core

# リストア
dotnet restore

# デバッグビルド
dotnet build

# リリースビルド（ポータブルEXE生成）
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

### 生成されるファイル

```
src\RukiCheck\bin\Release\net8.0-windows\win-x64\publish\RukiCheck.exe
```

このEXEを配布します（単体で動作）。

---

## 📊 report.json 例

```json
{
  "meta": {
    "management_id": "RT-250903",
    "inspection_date": "2025-09-22",
    "inspection_time": "14:35:12",
    "mode": "standard",
    "tool_version": "v0.1.0"
  },
  "storage": {
    "drive": "C:",
    "total_gb": 476.9,
    "free_gb": 18.2,
    "free_percent": 3.8,
    "status": "danger",
    "recommendation": "SSD容量アップ推奨（例：1TB）"
  },
  "keyboard": {
    "layout": "JIS",
    "total_keys": 109,
    "pressed_keys": 108,
    "missing_keys": ["F12"],
    "result": "warn"
  }
}
```

---

## 🔒 セキュリティとプライバシー

### 安全性

- ✅ レジストリ永続書き込みなし
- ✅ サービス・ドライバ追加なし
- ✅ 常駐・自動起動なし
- ✅ ネットワーク通信なし
- ✅ TEMP使用は実行中のみ（終了時自動削除）

### プライバシー

- カメラ/マイクは**検査実行時のみ**アクセス
- Windowsプライバシー設定で拒否されている場合はエラー表示
- 録音/撮影データはローカル保存のみ（外部送信なし）

---

## ⚠️ 制限事項（v0.1）

### 未実装項目

- SMART / NVMe health情報
- ファン回転数・CPU温度（取得困難）
- BitLockerキー取得
- メモリ完全テスト
- USBブート機能

### 既知の問題

- カメラ/マイクはWindowsプライバシー設定で許可が必要
- 一部のノートPCではFnキーが検出不可（仕様）
- 低スペックPCではCPUテストに時間がかかる場合あり

---

## 📝 ライセンス

MIT License - 自由に使用・改変・配布可能

### 使用ライブラリ

| ライブラリ | 用途 | ライセンス |
|-----------|------|-----------|
| NAudio | 音声録音・再生 | MIT |
| OpenCvSharp4 | カメラキャプチャ | Apache 2.0 |
| Microsoft.Extensions.DependencyInjection | DI | MIT |

---

## 🤝 コントリビュート

Pull Request歓迎！以下の方針でお願いします：

- 標準ユーザー権限で動作することを最優先
- 外部依存を増やさない
- ポータブル性を維持

---

## 📞 サポート

- **Issues**: [GitHub Issues](https://github.com/yourusername/rukicheck-core/issues)
- **ドキュメント**: [ARCHITECTURE.md](./ARCHITECTURE.md)
- **実装ノート**: [IMPLEMENTATION_NOTES.md](./IMPLEMENTATION_NOTES.md)

---

## 🎉 完成条件（DONE）

- [x] 標準ユーザーでEXEが起動
- [x] 管理番号＋日付でフォルダ生成
- [x] report.json / report.html が生成される
- [x] USBを抜けばPC側に何も残らない
- [ ] 実機5台以上でテスト完走 ← **次のステップ**

---

**Made with ❤️ for Windows PC Inspection**
