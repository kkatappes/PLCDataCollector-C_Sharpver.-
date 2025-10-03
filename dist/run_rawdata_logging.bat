@echo off
chcp 932 >nul
echo ================================================
echo        SLMP Raw Data Logging Script
echo ================================================
echo.
echo This script runs andon.exe with raw data
echo analysis and logging features enabled.
echo.
echo Log output destinations:
echo - Raw Data Log: logs/rawdata_analysis.log
echo - Raw Data JSON: logs/rawdata_analysis.json
echo - Terminal Output: logs/terminal_output.txt
echo.
echo ================================================
echo.

rem logsディレクトリを作成（存在しない場合）
if not exist logs mkdir logs

rem Execute andon.exe in continuous monitoring mode
echo Running andon.exe in continuous monitoring mode...
echo.
echo Press Ctrl+C to stop continuous execution
echo.
andon.exe

echo.
echo ================================================
echo Execution completed.
echo.
echo Please check log files:
if exist logs\rawdata_analysis.log (
    echo [OK] Raw data log file: logs\rawdata_analysis.log created
) else (
    echo [ERROR] Raw data log file was not created
)

if exist logs\rawdata_analysis.json (
    echo [OK] Raw data JSON file: logs\rawdata_analysis.json created
) else (
    echo [ERROR] Raw data JSON file was not created
)

if exist logs\terminal_output.txt (
    echo [OK] Terminal output file: logs\terminal_output.txt created
) else (
    echo [ERROR] Terminal output file was not created
)
echo ================================================
pause