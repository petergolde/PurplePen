rem @echo off
if "%1"=="" goto MissingArg

pushd ..\..\..\..\..\Programs\DotSpatialProjectionsLite
git checkout %1
popd 

if errorlevel 1 goto CheckoutFailed

echo "Copying..."
copy "..\..\..\..\..\Programs\DotSpatialProjectionsLite\bin\Debug\DotSpatial.Projections.dll" Debug
copy "..\..\..\..\..\Programs\DotSpatialProjectionsLite\bin\Debug\DotSpatial.Projections.pdb" Debug
copy "..\..\..\..\..\Programs\DotSpatialProjectionsLite\bin\Release\DotSpatial.Projections.dll" Release
copy "..\..\..\..\..\Programs\DotSpatialProjectionsLite\bin\Release\DotSpatial.Projections.pdb" Release


goto End

:MissingArg
Echo "Pass name of branch to copy from (e.g., "master")
goto End

:CheckoutFailed
Echo "Checkout of branch '%1' failed"
goto End

:End