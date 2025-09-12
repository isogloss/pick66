@echo off
setlocal ENABLEDELAYEDEXPANSION

:: ============================================================
::  exception.bat - Generic installer/build bootstrap script
::  - Elevates to Administrator if required
::  - Detects architecture
::  - Locates a .sln or .csproj
::  - Restores & builds with dotnet (if available)
::  - Logs all actions
::  - Optional: run the built app
:: ============================================================

:: --------------- Configuration (adjust if needed) ----------
set LOGFILE=installer.log
set BUILD_CONFIG=Release
set RUN_AFTER_BUILD=0
set DOTNET_ARGS=
:: If you know the target project/solution name, hardcode it:
set FORCE_SOLUTION=
set FORCE_PROJECT=
:: -----------------------------------------------------------

:: Timestamp helper
for /f "tokens=1-3 delims=/: " %%a in ("%date%") do set TODAY=%date%
for /f "tokens=1-2 delims=:." %%a in ("%time%") do set NOWTIME=%%a-%%b
set TS=%TODAY%_%NOWTIME%

:: Redirect all output to console and log
if not exist "%LOGFILE%" (type nul > "%LOGFILE%")
echo ===========================================================>> "%LOGFILE%"
echo Session Start %TS% >> "%LOGFILE%"
echo ===========================================================>> "%LOGFILE%"

call :log "Starting exception.bat"

:: Check admin
>nul 2>&1 net session
if %errorlevel% NEQ 0 (
  call :log "Not elevated. Attempting elevation..."
  powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "Start-Process -FilePath '%~f0' -Verb RunAs"
  if %errorlevel% NEQ 0 (
    call :log "Elevation failed or canceled."
    exit /b 1
  )
  exit /b 0
)

call :log "Running with administrative privileges."

:: Determine architecture
set ARCH=
if /i "%PROCESSOR_ARCHITECTURE%"=="AMD64" set ARCH=x64
if /i "%PROCESSOR_ARCHITECTURE%"=="ARM64" set ARCH=arm64
if /i "%PROCESSOR_ARCHITECTURE%"=="x86" set ARCH=x86
if "%ARCH%"=="" set ARCH=unknown
call :log "Detected architecture: %ARCH%"

:: Find solution / project
set TARGET_SOLUTION=
set TARGET_PROJECT=

if defined FORCE_SOLUTION (
  if exist "%FORCE_SOLUTION%" (
    set TARGET_SOLUTION=%FORCE_SOLUTION%
    call :log "Using forced solution: %TARGET_SOLUTION%"
  ) else (
    call :log "Forced solution not found: %FORCE_SOLUTION%"
  )
)

if not defined TARGET_SOLUTION (
  for /f "delims=" %%f in ('dir /b /a:-d *.sln 2^>nul') do (
    if not defined TARGET_SOLUTION set TARGET_SOLUTION=%%f
  )
  if defined TARGET_SOLUTION (
    call :log "Discovered solution: %TARGET_SOLUTION%"
  )
)

if defined FORCE_PROJECT (
  if exist "%FORCE_PROJECT%" (
    set TARGET_PROJECT=%FORCE_PROJECT%
    call :log "Using forced project: %TARGET_PROJECT%"
  ) else (
    call :log "Forced project not found: %FORCE_PROJECT%"
  )
)

if not defined TARGET_PROJECT (
  for /f "delims=" %%f in ('dir /b /s /a:-d *.csproj 2^>nul') do (
    if not defined TARGET_PROJECT set TARGET_PROJECT=%%f
  )
  if defined TARGET_PROJECT (
    call :log "Discovered project: %TARGET_PROJECT%"
  )
)

if not defined TARGET_SOLUTION if not defined TARGET_PROJECT (
  call :log "No .sln or .csproj found in current directory tree."
  call :log "Nothing to build."
  goto :end
)

:: Check dotnet availability
where dotnet >nul 2>&1
if errorlevel 1 (
  call :log "dotnet SDK not found in PATH."
  call :log "Attempting to continue without build."
  goto :end
) else (
  call :log "dotnet SDK detected."
)

:: Restore & build
if defined TARGET_SOLUTION (
  call :log "Restoring solution..."
  dotnet restore "%TARGET_SOLUTION%" %DOTNET_ARGS% >> "%LOGFILE%" 2>&1
  if errorlevel 1 (
    call :log "Restore failed."
    goto :error
  )
  call :log "Building solution..."
  dotnet build "%TARGET_SOLUTION%" -c %BUILD_CONFIG% --no-restore %DOTNET_ARGS% >> "%LOGFILE%" 2>&1
  if errorlevel 1 (
    call :log "Build failed."
    goto :error
  )
) else (
  call :log "Restoring project..."
  dotnet restore "%TARGET_PROJECT%" %DOTNET_ARGS% >> "%LOGFILE%" 2>&1
  if errorlevel 1 (
    call :log "Restore failed."
    goto :error
  )
  call :log "Building project..."
  dotnet build "%TARGET_PROJECT%" -c %BUILD_CONFIG% --no-restore %DOTNET_ARGS% >> "%LOGFILE%" 2>&1
  if errorlevel 1 (
    call :log "Build failed."
    goto :error
  )
)

call :log "Build completed successfully."

:: Try to locate an executable in typical output paths
set BUILT_EXE=
for /r "%cd%" %%f in (*.exe) do (
  echo "%%f" | find /i "\bin\%BUILD_CONFIG%\" >nul
  if not errorlevel 1 (
    if /i not "%%~nxf"=="exception.bat" (
      if not defined BUILT_EXE set BUILT_EXE=%%f
    )
  )
)

if defined BUILT_EXE (
  call :log "Candidate executable: %BUILT_EXE%"
  if "%RUN_AFTER_BUILD%"=="1" (
    call :log "Launching executable..."
    start "" "%BUILT_EXE%"
  )
) else (
  call :log "No built executable located in expected paths."
)

goto :end

:error
call :log "Installation/build encountered an error."
exit /b 1

:log
set MSG=%~1
echo [%date% %time%] %MSG%
echo [%date% %time%] %MSG%>> "%LOGFILE%"
exit /b 0

:end
call :log "Installer script finished."
endlocal
exit /b 0