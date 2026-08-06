#!/usr/bin/env bash
cd "$(dirname "$0")" || exit

dotnet tool install --global DeepStrip
PATH=$PATH:~/.dotnet/tools

BloonsTD6=$(< ../btd6.targets sed -En 's:.*>(.*)</BloonsTD6>.*:\1:p')
DLLS=$(< ../btd6.targets sed -En 's:.*Reference Include="\$\(Il2CppAssemblies\)\\(.*\.dll)".*:\1:p')

IL2CPP="$BloonsTD6/MelonLoader/Il2CppAssemblies"

for dll in $DLLS
do
  REAL_DLL="$IL2CPP/$dll"
  STRIPPED_DLL="./$dll"

  # -i has to come after the positional args, and without it DeepStrip can't resolve
  # cross-assembly references, so the bigger DLLs all fall through to the full copy
  if deepstrip "$REAL_DLL" "$STRIPPED_DLL" -i "$IL2CPP"
  then
    echo Deep stripped "$REAL_DLL to $STRIPPED_DLL"
  else
    cp "$REAL_DLL" "$STRIPPED_DLL"
    echo COPIED UNSTRIPPED "$REAL_DLL to $STRIPPED_DLL"
  fi
done

read -r -n 1 -p "Press Any Key to exit"