@echo off
setlocal enabledelayedexpansion

:: Başlangıç portu ve instance sayısı
set START_PORT=30100
set INSTANCE_COUNT=1
set FTMPLUS_PATH=FTMPlus.dll
cd FTMPlus\bin\Debug\net8.0\
:: Uygulamanın var olup olmadığını kontrol et
if not exist "%FTMPLUS_PATH%" (
    echo Hata: %FTMPLUS_PATH% bulunamadı!
    exit /b 1
) 


:: 10 instance başlat
for /l %%i in (0,1,%INSTANCE_COUNT%) do ( 
    set /a PORT=%START_PORT% + %%i
    echo FTMPlus başlatılıyor: http://localhost:!PORT!  
    start cmd /k  "dotnet %FTMPLUS_PATH% -c ASPNETCORE_ENVIRONMENT=Development --applicationUrl=http://localhost:!PORT!"
    timeout /t 10 >nul 
)

echo Tüm instancelar başlatıldı!
