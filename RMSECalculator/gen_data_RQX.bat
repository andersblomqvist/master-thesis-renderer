@echo off
setlocal EnableDelayedExpansion

:: Define the base directory
:: Check if an argument is provided
if "%~1"=="" (
    echo Usage: gen_data_RQX.bat ^<RQX folder^>
    echo Example: gen_data_RQX.bat RQ1
    exit /b 1
)

:: Set the base directory from the argument
set "baseDir=%~1"

:: Clear all .txt files in each folder within the base directory
for /d %%F in ("%baseDir%\*") do (
    del /q "%%F\*.txt" 2>nul
)

:: Loop through each folder in the base directory
for /d %%F in ("%baseDir%\*") do (
    set "folderName=%%~nF"

    :: Execute the commands for each region (1 to 4) and for all regions
    for %%R in (1 2 3 4) do (
        echo Running: rmse.exe !baseDir!\!folderName! %%R
        rmse.exe !baseDir!\!folderName! %%R
    )
    echo Running: rmse.exe !baseDir!\!folderName!
    rmse.exe !baseDir!\!folderName!
)

echo All tasks completed.
pause
