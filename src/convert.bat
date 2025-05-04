@echo off
if not exist input mkdir input
if not exist output mkdir output
cd input
for /R %%f in (*.fur) do (
    "%~dp0Fur2Uge.exe" --i "%%f" --o "..\output\%%~nf.uge"
)
pause
