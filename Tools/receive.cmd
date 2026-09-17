@echo off
cd /d "%~dp0"
python receive_udp.py %*
if errorlevel 1 pause
