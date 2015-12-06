set TOOLS=..\..\bin\

cd "unlock all rebel drops"
mkdir "dropzone"
mkdir "dropzone\profile"
copy "..\gibbed mod readme.txt" . /y
%TOOLS%\Gibbed.JustCause3.ConvertTask.exe "task.json" "dropzone\profile\task.onlinec"
del "..\unlock all rebel drops.zip"
7z a -r -tzip -mx=9 "..\unlock all rebel drops.zip" "dropzone" "gibbed mod readme.txt"
cd ..

cd "unlock all + hidden rebel drops"
mkdir "dropzone"
mkdir "dropzone\profile"
copy "..\gibbed mod readme.txt" . /y
%TOOLS%\Gibbed.JustCause3.ConvertTask.exe "task.json" "dropzone\profile\task.onlinec" 
del "..\unlock all + hidden rebel drops.zip"
7z a -r -tzip -mx=9 "..\unlock all + hidden rebel drops.zip" "dropzone" "gibbed mod readme.txt"
cd ..

cd "unlock hidden rebel drops"
mkdir "dropzone"
mkdir "dropzone\profile"
copy "..\gibbed mod readme.txt" . /y
%TOOLS%\Gibbed.JustCause3.ConvertTask.exe "task.json" "dropzone\profile\task.onlinec" 
del "..\unlock hidden rebel drops.zip"
7z a -r -tzip -mx=9 "..\unlock hidden rebel drops.zip" "dropzone" "gibbed mod readme.txt"
cd ..

cd "no rebel drop timers"
mkdir "dropzone"
mkdir "dropzone\profile"
copy "..\gibbed mod readme.txt" . /y
"%TOOLS%\Gibbed.JustCause3.ConvertItem.exe" "item.json" "dropzone\profile\item.onlinec" 
del "..\no rebel drop timers.zip"
7z a -r -tzip -mx=9 "..\no rebel drop timers.zip" "dropzone" "gibbed mod readme.txt"
cd ..

cd "enable hidden rebel drops"
mkdir "dropzone"
mkdir "dropzone\profile"
copy "..\gibbed mod readme.txt" . /y
"%TOOLS%\Gibbed.JustCause3.ConvertItem.exe" "item.json" "dropzone\profile\item.onlinec" 
del "..\enable hidden rebel drops.zip"
7z a -r -tzip -mx=9 "..\enable hidden rebel drops.zip" "dropzone" "gibbed mod readme.txt"
cd ..

cd "no rebel drop timers + enable hidden rebel drops"
mkdir "dropzone"
mkdir "dropzone\profile"
copy "..\gibbed mod readme.txt" . /y
"%TOOLS%\Gibbed.JustCause3.ConvertItem.exe" "item.json" "dropzone\profile\item.onlinec" 
del "..\no rebel drop timers + enable hidden rebel drops.zip"
7z a -r -tzip -mx=9 "..\no rebel drop timers + enable hidden rebel drops.zip" "dropzone" "gibbed mod readme.txt"
cd ..

cd "no intro"
copy "..\gibbed mod readme.txt" . /y
del "..\no intro.zip"
7z a -r -tzip -mx=9 "..\no intro.zip" "dropzone" "gibbed mod readme.txt"
cd ..

cd "infinite beacons"
mkdir "dropzone"
mkdir "dropzone\editor"
mkdir "dropzone\editor\entities"
mkdir "dropzone\editor\entities\jc_weapons"
mkdir "dropzone\editor\entities\jc_weapons\03_thrown"
mkdir "dropzone\editor\entities\jc_weapons\03_thrown\w302_beacon"
copy "..\gibbed mod readme.txt" . /y
"%TOOLS\Gibbed.JustCause3.ConvertProperty" "w302_beacon_modified.xml" "w302_beacon_modified.epe"
"%TOOLS\Gibbed.JustCause3.SmallPack.exe" -v "sarc" "dropzone\editor\entities\jc_weapons\03_thrown\w302_beacon\w302_beacon.ee"
del "..\infinite beacons.zip"
7z a -r -tzip -mx=9 "..\infinite beacons.zip" "dropzone" "gibbed mod readme.txt"
cd ..


pause
