mkdir output
echo %1
"c:\program files (x86)\html help workshop\hhc" "..\..\doc\userdocs\help\Purple Pen Help.hhp"
copy "output\Purple Pen Help.chm" ..\PurplePen\bin\%1
set ERRORLEVEL=0
