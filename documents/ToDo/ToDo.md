・C:\Users\1010821\Desktop\python\andon\documents\design\read_random実装\実装計画\Phase12_ProcessedDeviceRequestInfo恒久対策.md　やり残し
・利用されていない設定の確認
・実機テスト確認
・各ステップのフローごとの情報繋がり整理
・コミット


● 結論と推奨事項

  現在の設定ファイル利用方法

  Excelファイル(.xlsx)は単一実行ファイルに埋め込まれません。以下の方法で利用します:

  方法1: 実行ファイルと同じフォルダに配置（推奨）

  本番環境/
  ├── andon.exe
  ├── plc1_config.xlsx    // ← ConfigurationLoaderExcelが自動検出
  ├── plc2_config.xlsx    // ← ConfigurationLoaderExcelが自動検出
  └── appsettings.json    // ← オプション（システム設定）

  - ConfigurationLoaderExcel.DiscoverExcelFiles()がAppContext.BaseDirectoryから*.xlsxを自動検索
  - 複数のExcelファイルを配置可能（複数PLC対応）

  方法2: 相対パスで配置

  本番環境/
  ├── andon.exe
  ├── config/
  │   ├── plc1_config.xlsx
  │   └── plc2_config.xlsx
  └── appsettings.json

  この場合、コンストラクタで検索パスを指定する必要があります（現在の実装では要修正）。

  ビルドコマンド

  # 単一実行ファイル生成（Release版）
  dotnet publish andon/andon.csproj -c Release -o publish

  # 生成物
  publish/
  ├── andon.exe              // 単一実行ファイル（約60-100MB）
  └── appsettings.json       // 自動コピーされる（下記設定追加が必要）

  必要な追加設定

  現在、appsettings.jsonがコピーされない可能性があります。.csprojに以下を追加することを推奨します:

  <ItemGroup>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

  利用手順まとめ

  1. ビルド実行: dotnet publish andon/andon.csproj -c Release -o publish
  2. 配布物準備:
    - andon.exe
    - appsettings.json（システム設定用）
    - Excelファイル（PLC設定用）
  3. 本番環境配置:
    - 上記3つのファイルを同じフォルダに配置
  4. 実行: andon.exeを実行すると自動的にExcelファイルを検索・読み込み

  注意点

  - Excelファイルは実行時に読み込み可能である必要があります（埋め込みリソースではない）
  - ConfigurationWatcherによる設定ファイル変更監視機能も実装済み（動的リロード可能）
  - Excel編集中（ロック状態）のファイルはスキップされます（IsFileLocked()実装済み）

  この方式により、設定変更時に再ビルド不要で、Excelファイルのみ差し替えることができます。
