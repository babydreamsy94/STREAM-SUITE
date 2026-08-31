$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

if (-not (Get-Command py -ErrorAction SilentlyContinue)) {
    throw "Python Launcher (py.exe) was not found. Install Python 3.12 for Windows first."
}

py -3.12 -m venv .venv
& .\.venv\Scripts\python.exe -m pip install --upgrade pip
& .\.venv\Scripts\python.exe -m pip install --requirement requirements-build.txt
& .\.venv\Scripts\python.exe -m unittest discover -s tests -v
& .\.venv\Scripts\python.exe -m PyInstaller --noconfirm --clean StreamSuiteUpdateCenter.spec

Write-Host ""
Write-Host "Build complete: dist\StreamSuiteUpdateCenter.exe" -ForegroundColor Green
