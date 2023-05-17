@echo off
setlocal

set "batch_dir=%~dp0"
cd /d "%batch_dir%x64"

start "" "%batch_dir%x64\GymnasieArbete.exe"

endlocal
