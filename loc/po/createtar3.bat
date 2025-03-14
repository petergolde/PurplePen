mkdir tar
mkdir tar\po
mkdir tar\purplepen
copy *.po tar\purplepen
copy *.pot tar\po

cd tar
tar -cf purplepen.tar purplepen\*.po po\*.pot
cd ..

