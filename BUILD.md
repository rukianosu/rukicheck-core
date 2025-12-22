# RukiCheck ビルド手順

## 前提条件

- **.NET 8 SDK** 以上
- **Windows環境**（Windows 10/11推奨）
- **Visual Studio 2022** または **VS Code**（オプション）

---

## 開発環境セットアップ

### 1. .NET SDK インストール

```bash
# バージョン確認
dotnet --version
# 8.0.x 以上であればOK
```

インストールされていない場合:
https://dotnet.microsoft.com/download/dotnet/8.0

### 2. リポジトリクローン

```bash
git clone https://github.com/yourusername/rukicheck-core.git
cd rukicheck-core
```

### 3. 依存パッケージ復元

```bash
dotnet restore
```

---

## ビルド方法

### デバッグビルド（開発用）

```bash
dotnet build
```

生成場所:
```
src\RukiCheck\bin\Debug\net8.0-windows\RukiCheck.exe
```

⚠️ **ポータブルEXEではありません**（依存DLLが別途必要）

---

### リリースビルド（配布用）

#### 方法1: 基本的なポータブルEXE生成

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

生成場所:
```
src\RukiCheck\bin\Release\net8.0-windows\win-x64\publish\RukiCheck.exe
```

#### 方法2: 圧縮・最適化版

```bash
dotnet publish -c Release -r win-x64 ^
  --self-contained true ^
  /p:PublishSingleFile=true ^
  /p:EnableCompressionInSingleFile=true ^
  /p:DebugType=None ^
  /p:DebugSymbols=false
```

→ EXEサイズが約30%削減されます

#### 方法3: ビルドスクリプト使用（推奨）

```bash
# Windows
.\build.bat

# PowerShell
.\build.ps1
```

---

## ビルドスクリプト

### build.bat

```batch
@echo off
echo ===================================
echo RukiCheck ポータブルEXE ビルド
echo ===================================

dotnet publish -c Release -r win-x64 ^
  --self-contained true ^
  /p:PublishSingleFile=true ^
  /p:EnableCompressionInSingleFile=true ^
  /p:DebugType=None ^
  /p:DebugSymbols=false

echo.
echo ビルド完了！
echo 出力: src\RukiCheck\bin\Release\net8.0-windows\win-x64\publish\RukiCheck.exe
pause
```

### build.ps1

```powershell
Write-Host "===================================" -ForegroundColor Cyan
Write-Host "RukiCheck ポータブルEXE ビルド" -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan

dotnet publish -c Release -r win-x64 `
  --self-contained true `
  /p:PublishSingleFile=true `
  /p:EnableCompressionInSingleFile=true `
  /p:DebugType=None `
  /p:DebugSymbols=false

Write-Host ""
Write-Host "ビルド完了！" -ForegroundColor Green
Write-Host "出力: src\RukiCheck\bin\Release\net8.0-windows\win-x64\publish\RukiCheck.exe" -ForegroundColor Yellow
```

---

## ビルド成果物

### ファイル構成

```
publish\
└── RukiCheck.exe  ← これ1つだけ！
```

### EXEサイズ

| ビルドオプション | サイズ目安 |
|-----------------|----------|
| 圧縮なし | 約80MB |
| 圧縮あり | 約50MB |

---

## デジタル署名（オプション）

配布前にコード署名を推奨:

```bash
signtool sign /f certificate.pfx /p password /t http://timestamp.digicert.com RukiCheck.exe
```

---

## Visual Studio でビルド

### 1. ソリューションを開く

```
RukiCheck.sln
```

### 2. ビルド構成を選択

- **Release | x64**

### 3. 発行

```
右クリック > RukiCheck > 発行
→ 発行プロファイルを作成（FolderProfile）
→ ターゲット: win-x64
→ 配置モード: 自己完結
→ 単一ファイルの生成: ✅
```

---

## トラブルシューティング

### Q1. ビルドエラー: "NAudio が見つかりません"

```bash
dotnet restore
```

### Q2. 実行時エラー: "OpenCvSharp のDLLが見つかりません"

→ `IncludeNativeLibrariesForSelfExtract` が有効か確認:

```xml
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
```

### Q3. EXEサイズが100MB以上

→ 圧縮オプションを有効化:

```bash
/p:EnableCompressionInSingleFile=true
```

---

## CI/CD（GitHub Actions例）

```yaml
name: Build RukiCheck

on:
  push:
    branches: [ main ]

jobs:
  build:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v3
    - uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    - run: dotnet restore
    - run: dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
    - uses: actions/upload-artifact@v3
      with:
        name: RukiCheck.exe
        path: src/RukiCheck/bin/Release/net8.0-windows/win-x64/publish/RukiCheck.exe
```

---

## リリースチェックリスト

配布前に確認すること:

- [ ] Release構成でビルド
- [ ] 単一EXEであることを確認
- [ ] 標準ユーザーで実行テスト
- [ ] ウイルススキャン（VirusTotal等）
- [ ] デジタル署名（推奨）
- [ ] READMEとバージョン番号を更新
- [ ] GitHubリリースページを作成

---

**ビルド完了後、必ず実機テストを行ってください！**
