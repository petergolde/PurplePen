del purplepen-setup.7z
del purplepen-setup.exe
..\tools\7zr a -t7z purplepen-setup.7z setup.exe ..\tools\RunAndWait.exe purplepen.msi -m0=BCJ2 -m1=LZMA:d25:fb255 -m2=LZMA:d19 -m3=LZMA:d19 -mb0:1 -mb0s1:2 -mb0s2:3 -mx 
copy /b ..\tools\7zSD.sfx + ..\tools\config.txt + purplepen-setup.7z purplepen-setup.exe
