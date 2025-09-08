@echo off
setlocal enabledelayedexpansion

:: Check if version argument is provided
if "%~1"=="" (
    echo Version number is required.
    echo Usage: build.bat [version] [extra_args...]
    exit /b 1
)

:: Store the version number
set "version=%~1"

:: Shift arguments to process additional args
shift

:: Define common paths
set "scriptDir=%~dp0"
set "publishDir=%scriptDir%publish"
set "releaseDir=%scriptDir%releases"
set "exeName=OSFR Launcher.exe"

:: Compile Launcher with dotnet
echo.
echo Compiling Launcher with dotnet...
echo %scriptDir%
dotnet publish "%scriptDir%src\Launcher.sln" -c Release --no-self-contained -r win-x64 --property:PublishDir="%publishDir%"

:: Verify that Launcher.exe exists
if not exist "%publishDir%\OSFR Launcher.exe" (
    echo ERROR: Launcher.exe not found in %publishDir%
    exit /b 1
)

:: Verify that App.ico exists
if not exist "%scriptDir%src\Launcher\App.ico" (
    echo ERROR: App.ico not found in %scriptDir%
    exit /b 1
)

:: Build Velopack Release
echo.
echo Building Velopack Release v%version%
vpk pack --packTitle "OSFR Launcher" --packAuthors "OSFR Team" ^
  -u OSFRLauncher ^
  -e "%exeName%" ^
  -o "%releaseDir%" ^
  -p "%publishDir%" ^
  -i "%scriptDir%src\Launcher\App.ico" ^
  -f net9-x64-desktop ^
  -v %*

pause