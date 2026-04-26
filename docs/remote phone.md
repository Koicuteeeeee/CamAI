@echo off
title Ket noi Samsung A03s tu xa
cls

:: 1. Thiet lap thong so
set IP_PHONE=100.120.105.33
set PORT=5555

echo [INFO] Dang kiem tra ket noi den %IP_PHONE%...

:: 2. Reset ADB va ket noi
adb kill-server
adb start-server
echo [INFO] Dang thu ket noi den %IP_PHONE%:%PORT%...
adb connect %IP_PHONE%:%PORT%

:: Doi 2 giay de ADB on dinh
timeout /t 2 >nul

:: 3. Chay Scrcpy
echo [INFO] Dang khoi chay Scrcpy...
:: Them tham so --no-audio neu ong chi muon xem hinh cho nhe luong
scrcpy -s %IP_PHONE%:%PORT% --video-bit-rate 2M --max-fps 30 --max-size 1024 --always-on-top

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Khong the mo Scrcpy. Hay kiem tra:
    echo 1. Dien thoai da bat 'Go loi khong day' (Wireless Debugging) chua?
    echo 2. IP %IP_PHONE% co dung khong? (Check trong Tailscale)
    echo 3. Da chay lenh 'adb tcpip 5555' qua cap USB chua?
)

pause