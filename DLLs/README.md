# btd6-ci-dependencies

This repo contains the necessary DLLs for compiling BloonsTD6 mods in continuous integrations systems i.e. GitHub actions.

The [DeepStrip](https://github.com/ash-zsh/DeepStrip) utility is used to minimize DLL size and remove all chances of hosting any Ninja Kiwi copyrighted information (although the DLLs produced by MelonLoader are somewhat stripped already).

MelonLoader DLLs are not included, as those can be installed in a separate step from their [Releases](https://github.com/LavaGang/MelonLoader/releases) / [GitHub Actions](https://github.com/LavaGang/MelonLoader/actions). Currently, only the BTD6 DLLs referenced by the Mod Helper's `btd6.targets` file are included. 

## Updating

`git clone` this repository inside your own BTD6 Mod Sources folder, so that there's a `btd6.targets` next to the `btd6-ci-dependencies` folder. Then, run `update.bat` / `update.sh`