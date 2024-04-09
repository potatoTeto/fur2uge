@echo off
if not exist input mkdir input
if not exist output mkdir output
cd input
for /R %%f in (*.fur) do %~dp0/Fur2Uge.exe "%%f" "../output/%%~nf.uge"
pause