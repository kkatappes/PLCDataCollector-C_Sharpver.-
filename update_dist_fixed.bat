@echo off
chcp 932 >nul
echo ====================================
echo         Andon.exe Update Script
echo ====================================
echo.

echo Building project...
cd /d C:\Users\1010821\Desktop\python\andon\andon
dotnet build --configuration Release

echo.
echo Copying files to dist folder...
copy "bin\Release\net9.0\andon.exe" "..\dist\andon.exe"
copy "bin\Release\net9.0\andon.dll" "..\dist\andon.dll" 2>nul
copy "bin\Release\net9.0\andon.pdb" "..\dist\andon.pdb" 2>nul

echo.
echo Creating logs directory if it doesn't exist...
cd /d C:\Users\1010821\Desktop\python\andon\dist
if not exist logs mkdir logs

echo.
echo Update completed successfully!
echo ====================================
pause