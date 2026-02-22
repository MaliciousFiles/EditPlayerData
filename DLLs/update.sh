#!/usr/bin/env bash
cd "$(dirname "$0")" || exit

dotnet tool install --global DeepStrip
PATH=$PATH:~/.dotnet/tools

BloonsTD6=$(< ../btd6.targets sed -En 's:.*>(.*)</BloonsTD6>.*:\1:p')
DLLS=$(< ../btd6.targets sed -En 's:.*Reference Include="\$\(Il2CppAssemblies\)\\(.*\.dll)".*:\1:p')

for dll in $DLLS
do
  REAL_DLL="$BloonsTD6/MelonLoader/Il2CppAssemblies/$dll"
  STRIPPED_DLL="./$dll"
  
  deepstrip "$REAL_DLL" "$STRIPPED_DLL" || cp "$REAL_DLL" "$STRIPPED_DLL"
  echo Deep stripped "$REAL_DLL to $STRIPPED_DLL"
done

read -r -n 1 -p "Press Any Key to exit"