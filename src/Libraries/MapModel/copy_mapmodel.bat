rem @echo off
if "%1"=="" goto MissingArg

pushd ..\..\..\..\..\Programs\MapModel
git checkout %1
popd 

if errorlevel 1 goto CheckoutFailed

echo "Copying..."
copy "..\..\..\..\..\Programs\MapModel\src\MapModel\bin\Debug\MapModel.dll" Debug
copy "..\..\..\..\..\Programs\MapModel\src\MapModel\bin\Debug\MapModel.pdb" Debug
copy "..\..\..\..\..\Programs\MapModel\src\MapModel\bin\Release\MapModel.dll" Release
copy "..\..\..\..\..\Programs\MapModel\src\MapModel\bin\Release\MapModel.pdb" Release
copy "..\..\..\..\..\Programs\MapModel\src\Map_GDIPlus\bin\Debug\Map_GDIPlus.dll" Debug
copy "..\..\..\..\..\Programs\MapModel\src\Map_GDIPlus\bin\Debug\Map_GDIPlus.pdb" Debug
copy "..\..\..\..\..\Programs\MapModel\src\Map_GDIPlus\bin\Release\Map_GDIPlus.dll" Release
copy "..\..\..\..\..\Programs\MapModel\src\Map_GDIPlus\bin\Release\Map_GDIPlus.pdb" Release
copy "..\..\..\..\..\Programs\MapModel\src\Map_GDIPlus\bin\Debug\GDIPlusNative.dll" Debug
copy "..\..\..\..\..\Programs\MapModel\src\Map_GDIPlus\bin\Release\GDIPlusNative.dll" Release
copy "..\..\..\..\..\Programs\MapModel\src\Map_WPF\bin\Debug\Map_WPF.dll" Debug
copy "..\..\..\..\..\Programs\MapModel\src\Map_WPF\bin\Debug\Map_WPF.pdb" Debug
copy "..\..\..\..\..\Programs\MapModel\src\Map_WPF\bin\Release\Map_WPF.dll" Release
copy "..\..\..\..\..\Programs\MapModel\src\Map_WPF\bin\Release\Map_WPF.pdb" Release
copy "..\..\..\..\..\Programs\MapModel\src\Map_PDF\bin\Debug\Map_PDF.dll" Debug
copy "..\..\..\..\..\Programs\MapModel\src\Map_PDF\bin\Debug\Map_PDF.pdb" Debug
copy "..\..\..\..\..\Programs\MapModel\src\Map_PDF\bin\Release\Map_PDF.dll" Release
copy "..\..\..\..\..\Programs\MapModel\src\Map_PDF\bin\Release\Map_PDF.pdb" Release
copy "..\..\..\..\..\Programs\MapModel\src\PdfSharp\bin\Debug\PdfSharp.dll" Debug
copy "..\..\..\..\..\Programs\MapModel\src\PdfSharp\bin\Debug\PdfSharp.pdb" Debug
copy "..\..\..\..\..\Programs\MapModel\src\PdfSharp\bin\Release\PdfSharp.dll" Release
copy "..\..\..\..\..\Programs\MapModel\src\Graphics2D\bin\Debug\Graphics2D.dll" Debug
copy "..\..\..\..\..\Programs\MapModel\src\Graphics2D\bin\Debug\Graphics2D.pdb" Debug
copy "..\..\..\..\..\Programs\MapModel\src\Graphics2D\bin\Release\Graphics2D.dll" Release
copy "..\..\..\..\..\Programs\MapModel\src\Graphics2D\bin\Release\Graphics2D.pdb" Release

goto End

:MissingArg
Echo "Pass name of branch to copy from (e.g., "trunk")
goto End

:CheckoutFailed
Echo "Checkout of branch '%1' failed"
goto End

:End