@echo off
REM Mac 서버 빌드 스크립트
REM 사용법: build-mac.bat [intel|arm|both]

echo ========================================
echo   Interplanetary Server - Mac Build
echo ========================================
echo.

set BUILD_TYPE=%1
if "%BUILD_TYPE%"=="" set BUILD_TYPE=both

cd BaseServer

if "%BUILD_TYPE%"=="intel" goto BUILD_INTEL
if "%BUILD_TYPE%"=="arm" goto BUILD_ARM
if "%BUILD_TYPE%"=="both" goto BUILD_BOTH

echo Invalid argument. Use: intel, arm, or both
goto END

:BUILD_INTEL
echo [1/1] Building for Mac Intel (osx-x64)...
dotnet publish -c Release -r osx-x64 --self-contained true -o ./publish/osx-x64
if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    goto END
)
echo.
echo Build completed: ./publish/osx-x64/
goto END

:BUILD_ARM
echo [1/1] Building for Mac Apple Silicon (osx-arm64)...
dotnet publish -c Release -r osx-arm64 --self-contained true -o ./publish/osx-arm64
if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    goto END
)
echo.
echo Build completed: ./publish/osx-arm64/
goto END

:BUILD_BOTH
echo [1/2] Building for Mac Intel (osx-x64)...
dotnet publish -c Release -r osx-x64 --self-contained true -o ./publish/osx-x64
if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    goto END
)
echo.

echo [2/2] Building for Mac Apple Silicon (osx-arm64)...
dotnet publish -c Release -r osx-arm64 --self-contained true -o ./publish/osx-arm64
if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    goto END
)
echo.
echo ========================================
echo   Build Completed Successfully!
echo ========================================
echo.
echo Intel Mac build: ./publish/osx-x64/
echo Apple Silicon build: ./publish/osx-arm64/
echo.
echo Next steps:
echo 1. Copy appsettings.json to the publish folder
echo 2. Transfer files to Mac server
echo 3. Run: chmod +x BaseServer
echo 4. Run: ./BaseServer
goto END

:END
cd ..
pause
