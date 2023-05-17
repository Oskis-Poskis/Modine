@echo off
setlocal

set "batch_dir=%~dp0"
cd /d "%batch_dir%x86-release"

start "" "%batch_dir%x86-release\GymnasieArbete.exe"

endlocal
