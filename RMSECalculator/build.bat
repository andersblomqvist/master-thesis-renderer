@echo off

set EXE_NAME=rmse
set PROJECT_DIR=.
set SRC_DIR=%PROJECT_DIR%\src

odin build src\ -microarch:native -collection:src=src -out:%EXE_NAME%.exe -o:speed  -no-bounds-check

if "%1" == "run" (
    %EXE_NAME%.exe C:\Users\ander\Documents\rmse-calculator\scene_1_blue_noise_ema_none
)
