@echo off
chcp 65001 >nul
echo ################################################################################
echo # PLC単一通信テストプログラム - ビルドスクリプト（フレーム解析機能付き）
echo ################################################################################
echo.

echo [ビルド] 単一実行ファイルを作成中...
echo   ソース: program_single.cs
echo   出力  : PlcSingleTest.exe
echo.

dotnet publish PlcSingleTest.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false

echo.
if %ERRORLEVEL% EQU 0 (
    echo ################################################################################
    echo [完了] ビルド成功！
    echo ################################################################################
    echo.
    echo 実行ファイル:
    echo   bin\Release\net6.0\win-x64\publish\PlcSingleTest.exe
    echo.
    echo この実行ファイルは他のPCで単独動作します（.NET不要）
    echo.
    echo 【追加された解析機能】
    echo   - 3E/4Eフレームの自動判定（サブヘッダから識別）
    echo   - ヘッダ情報の詳細表示
    echo   - 終了コードの正常/エラー判定
    echo   - デバイスデータのHEXダンプ
    echo.
) else (
    echo ################################################################################
    echo [エラー] ビルド失敗
    echo ################################################################################
    echo.
)

pause
