@echo off

set EXE_NAME=rmse
set PROJECT_DIR=.
set SRC_DIR=%PROJECT_DIR%\src

odin build src\ -microarch:native -collection:src=src -out:%EXE_NAME%.exe -o:speed  -no-bounds-check
