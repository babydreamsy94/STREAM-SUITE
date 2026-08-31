@echo off
setlocal
cd /d "%~dp0"
py -3.12 src\stream_suite_updater.py %*
if errorlevel 1 pause
