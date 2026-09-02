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
set BETA=1

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

%INNOSETUP_EXECUTABLE% "%INNOSETUP_FILE%" /DBeta=%BETA%

if errorlevel 1 (
    echo.
    echo ERROR: Innosetup failed. Setup was not created.
    exit /b 1
)

echo Done creating setup program.