@echo off

set INNOSETUP_EXECUTABLE="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
set INNOSETUP_FILE=PurplePen.iss
set PROJECT_FILE=..\AvPurplePen\AvPurplePen.csproj
set PDFCONVERTER_PROJECT_FILE=..\PdfConverter\PdfConverter.csproj
set RUNTIME_IDENTIFIER=win-x64
set CONFIGURATION=Release
set TARGET_FRAMEWORK=net10.0
set SELF_CONTAINED=true
set PUBLISH_READYTORUN=false

rem GetVersion.cs reads the version number out of a compiled assembly and writes
rem a batch file that sets VERSION_MAJOR/MINOR/BUILD/REV, VERSION_STRING,
rem VERSION_PRERELEASE and SETUP_BASENAME. Those drive the Beta, MyAppVersion and
rem MyOutputBase defines that PurplePen.iss compiles with, so the installer's
rem name, version and beta-vs-stable identity all follow from the one version
rem number in VersionNumber.cs.
set GETVERSION_SOURCE=..\Installer\GetVersion.cs
set VERSION_DLL=publish\Main\PurplePenCore.dll
set VERSION_SCRIPT=publish\setversion.cmd

rmdir /s /q publish 2>nul

mkdir publish
mkdir publish\Main
mkdir publish\PdfConverter

dotnet publish "%PROJECT_FILE%" --configuration "%CONFIGURATION%" --runtime "%RUNTIME_IDENTIFIER%" --framework "%TARGET_FRAMEWORK%" --self-contained "%SELF_CONTAINED%" --output "publish\Main" -p:PublishReadyToRun="%PUBLISH_READYTORUN%" -p:UseAppHost=true -p:DebugType=none --nologo

if errorlevel 1 (
    echo.
    echo ERROR: dotnet publish failed. Setup was not created.
    exit /b 1
)

dotnet publish "%PDFCONVERTER_PROJECT_FILE%" --configuration "%CONFIGURATION%" --runtime "%RUNTIME_IDENTIFIER%" --framework "%TARGET_FRAMEWORK%" --self-contained "%SELF_CONTAINED%" --output "publish\PdfConverter" -p:PublishReadyToRun="%PUBLISH_READYTORUN%" -p:UseAppHost=true -p:DebugType=none --nologo

if errorlevel 1 (
    echo.
    echo ERROR: dotnet publish failed. Setup was not created.
    exit /b 1
)

copy publish\PdfConverter\PdfConverter*.* publish\Main
copy publish\PdfConverter\Pdfium*.* publish\Main

dotnet run --file "%GETVERSION_SOURCE%" -- batch "%VERSION_DLL%" > "%VERSION_SCRIPT%"

if errorlevel 1 (
    echo.
    echo ERROR: Could not read the version number from "%VERSION_DLL%". Setup was not created.
    exit /b 1
)

call "%VERSION_SCRIPT%"

if not defined SETUP_BASENAME (
    echo.
    echo ERROR: "%VERSION_SCRIPT%" did not set the version variables. Setup was not created.
    exit /b 1
)

echo Creating setup for version %VERSION_STRING% as %SETUP_BASENAME%.exe

%INNOSETUP_EXECUTABLE% "%INNOSETUP_FILE%" /DBeta=%VERSION_PRERELEASE% /DMyAppVersion=%VERSION_STRING% /DMyOutputBase=%SETUP_BASENAME%

if errorlevel 1 (
    echo.
    echo ERROR: Innosetup failed. Setup was not created.
    exit /b 1
)

echo Done creating setup program.