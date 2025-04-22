@echo off
setlocal

:: Check if source folder is provided
if "%~1"=="" (
    echo Usage: %~nx0 SourceFolder
    goto :eof
)

set "SOURCE=%~1"
set "DEST=extracted_txt_files"

if not exist "%DEST%" (
    mkdir "%DEST%"
)

:: find and copy all .txt files into DEST
for /R "%SOURCE%" %%F in (*.txt) do (
    copy "%%F" "%DEST%\"
)

echo Done copying .txt files from "%SOURCE%" to "%DEST%".
