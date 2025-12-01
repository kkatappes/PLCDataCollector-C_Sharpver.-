# PLC通信テストプログラム ビルド手順書

## 更新日時
2025-11-13

## 概要
andonプロジェクトのフレーム解析機能を実装したPLC通信テストプログラムのビルド手順です。

## プログラム構成

### PlcSingleTest.exe（単一通信テスト・詳細解析版）
**ソースファイル**: program_single.cs
**プロジェクトファイル**: PlcSingleTest.csproj
**ビルドスクリプト**: build_single.bat

**機能**:
- settings_decimal.txtの設定で1回だけPLC通信を実行
- 受信フレームの詳細解析と表示
  - フレームタイプ判定（3E/4E）
  - ヘッダ情報の完全表示
  - 終了コードの正常/エラー判定
  - デバイスデータのHEXダンプ

**用途**: 通信テスト、フレーム構造の確認、デバッグ

## ビルド方法

```
build_single.bat
```

## ビルド出力先
```
bin\Release\net6.0\win-x64\publish\PlcSingleTest.exe
```

## 実行ファイルの特徴
- **.NETランタイム不要**: 自己完結型（self-contained）
- **単一ファイル**: 全ての依存関係を含む
- **Windows x64専用**: win-x64ターゲット

## 実行に必要なファイル

```
PlcSingleTest.exe
settings_decimal.txt  ← 送信データ（10進数カンマ区切り）
```

## 追加された解析機能詳細

### フレームタイプ自動判定
サブヘッダバイトから3E/4Eフレームを自動識別します。

| フレームタイプ | サブヘッダ（バイト順） | 説明 |
|--------------|---------------------|------|
| 3E           | 0xD0 0x00          | 3Eフレーム応答 |
| 4E           | 0xD4 0x00          | 4Eフレーム応答 |

### 3Eフレーム構造解析
```
Idx 0-1  : サブヘッダ（0xD0 0x00）
Idx 2    : ネットワーク番号
Idx 3    : PC番号
Idx 4-5  : I/O番号（リトルエンディアン）
Idx 6    : 局番
Idx 7-8  : データ長（リトルエンディアン）
Idx 9-10 : 終了コード（リトルエンディアン）
Idx 11~  : デバイスデータ
```

### 4Eフレーム構造解析
```
Idx 0-1   : サブヘッダ（0xD4 0x00）
Idx 2     : ネットワーク番号
Idx 3     : PC番号
Idx 4-5   : I/O番号（リトルエンディアン）
Idx 6     : 局番
Idx 7-8   : データ長（リトルエンディアン）
Idx 9-10  : 監視タイマ（リトルエンディアン）
Idx 11-12 : 終了コード（リトルエンディアン）
Idx 13~   : デバイスデータ
```

### 終了コード判定
- `0x0000`: 正常終了
- その他: エラー（エラーコード表示）

## トラブルシューティング

### ビルドエラー: .NET SDKが見つからない
```
dotnet --version
```
で.NET 6.0以降がインストールされているか確認してください。

### ビルドエラー: NuGetパッケージが見つからない
```
dotnet restore PlcSingleTest.csproj
```
を実行してから再ビルドしてください。

### 実行エラー: settings_decimal.txtが見つからない
PlcSingleTest.exeと同じフォルダにsettings_decimal.txtを配置してください。

## 参考情報

### andonプロジェクトとの対応
このconmoni_testの解析機能は、andonプロジェクトの以下の実装を参考にしています。

- `andon/Core/Managers/PlcCommunicationManager.cs`
  - `ProcessReceivedRawData` (行864-985)
  - `DetectFrameTypeFromSubheader` (行1259-1288)
  - `Parse3EFrameStructure` (行1331-1365)
  - `Parse4EFrameStructure` (行1370-1408)

### 実装の違い
- **andon**: 完全な検証、エラーハンドリング、統計情報収集
- **conmoni_test**: 基本的な解析機能のみ（軽量・シンプル）

## 更新履歴
- 2025-11-13: 初版作成、フレーム解析機能追加
