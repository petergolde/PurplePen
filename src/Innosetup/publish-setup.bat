@echo off

rem publish-setup.bat
rem
rem Builds the Windows setup program and files it into the publishing tree: the
rem directory whose contents are uploaded to the download site, and whose layout
rem therefore has to match the URLs recorded in the update manifest.
rem
rem Three steps: create-setup.bat builds the installer and hands back the version
rem variables describing it, the installer is copied into the tree, and
rem UpdateManifest records it in manifest.json so that running copies of Purple
rem Pen are offered the update.

rem Both this script and create-setup.bat use paths relative to the Innosetup
rem directory, so make that the current directory rather than requiring the
rem script to be started from there.
cd /d "%~dp0"

rem The root of the publishing tree. This directory maps onto PUBLISH_URL_ROOT
rem once uploaded, which is where Purple Pen looks for manifest.json (see
rem AvPurplePen\UpdateManager.cs). The linux packages publish into the same tree;
rem their half of it is configured in Installer\LinuxInstaller\config.sh.
set "PUBLISH_TREE=C:\Users\peter\OneDrive\Purple Pen\Downloads\root"
set "PUBLISH_URL_ROOT=https://downloads.purple-pen.org"

rem Where the Windows installers live inside the tree. The directory to copy into
rem and the URL to record are both derived from this one setting -- if they
rem disagree, the manifest points at a file that isn't there and every update
rem fails the download, so there is deliberately only one place to change it.
rem Written with forward slashes for the URL; the path substitution turns them
rem into backslashes.
set "PUBLISH_SUBDIR=windows/x64"

set "PUBLISH_DIR=%PUBLISH_TREE%\%PUBLISH_SUBDIR:/=\%"
set "PUBLISH_URL_BASE=%PUBLISH_URL_ROOT%/%PUBLISH_SUBDIR%"
set "MANIFEST_FILE=%PUBLISH_TREE%\manifest.json"

set "UPDATEMANIFEST_PROJECT=..\Tools\UpdateManifest\UpdateManifest.csproj"
set "SETUP_OUTPUT_DIR=output"

rem Build the setup program. create-setup.bat leaves VERSION_STRING,
rem VERSION_PRERELEASE, SETUP_BASENAME and PROGRAM_TITLE set, along with
rem RUNTIME_IDENTIFIER, and those describe exactly what it just built.
call "%~dp0create-setup.bat"

if errorlevel 1 (
    echo.
    echo ERROR: create-setup.bat failed. Nothing was published.
    exit /b 1
)

if not defined SETUP_BASENAME (
    echo.
    echo ERROR: create-setup.bat did not set the version variables. Nothing was published.
    exit /b 1
)

set "SETUP_FILE=%SETUP_OUTPUT_DIR%\%SETUP_BASENAME%.exe"

if not exist "%SETUP_FILE%" (
    echo.
    echo ERROR: "%SETUP_FILE%" was not created. Nothing was published.
    exit /b 1
)

rem A prerelease goes on the beta channel, a final release on the main channel.
rem These are the names UpdateManager.GetChannels asks for: a build filed under
rem any other name is one no copy of Purple Pen will ever be offered.
if "%VERSION_PRERELEASE%"=="1" (
    set "PUBLISH_CHANNEL=beta"
) else (
    set "PUBLISH_CHANNEL=main"
)

if not exist "%PUBLISH_TREE%" (
    echo.
    echo ERROR: publishing tree "%PUBLISH_TREE%" does not exist. Nothing was published.
    exit /b 1
)

if not exist "%PUBLISH_DIR%" mkdir "%PUBLISH_DIR%"

echo.
echo Publishing %PROGRAM_TITLE% to "%PUBLISH_DIR%"

copy /y "%SETUP_FILE%" "%PUBLISH_DIR%\"

if errorlevel 1 (
    echo.
    echo ERROR: could not copy "%SETUP_FILE%" into "%PUBLISH_DIR%". Nothing was published.
    exit /b 1
)

rem Record the release in the manifest. --file is the copy in the publishing
rem tree rather than the one in the output directory, so that the hash written
rem into the manifest is the hash of the file that will actually be served.
rem
rem --platform is the runtime identifier the installer was built for, which is
rem the same name UpdateManager.GetPlatformName composes at run time; taking it
rem from the build rather than writing it out again keeps the two in step.
dotnet run --project "%UPDATEMANIFEST_PROJECT%" --configuration Release -- ^
    --manifest "%MANIFEST_FILE%" ^
    --title "%PROGRAM_TITLE%" ^
    --version "%VERSION_STRING%" ^
    --platform "%RUNTIME_IDENTIFIER%" ^
    --channel "%PUBLISH_CHANNEL%" ^
    --file "%PUBLISH_DIR%\%SETUP_BASENAME%.exe" ^
    --url-base "%PUBLISH_URL_BASE%"

if errorlevel 1 (
    echo.
    echo ERROR: UpdateManifest failed. The setup program was copied into the tree,
    echo but "%MANIFEST_FILE%" does not describe it, so no one will be offered it.
    exit /b 1
)

echo.
echo Done publishing %PROGRAM_TITLE%.
