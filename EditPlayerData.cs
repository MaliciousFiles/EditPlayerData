using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Extensions;
using EditPlayerData;
using EditPlayerData.UI;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.Settings;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(EditPlayerData.EditPlayerData), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace EditPlayerData;

public class EditPlayerData : BloonsTD6Mod
{

    public override void OnApplicationStart()
    {
        ModHelper.Msg<EditPlayerData>("EditPlayerData loaded!");
    }

    public override void OnProfileLoaded(ProfileModel result)
    {
        EditPlayerDataMenu.InitSettings(result);
    }

    [HarmonyPatch(typeof(SettingsScreen), nameof(SettingsScreen.Open))]
    // ReSharper disable once InconsistentNaming
    internal class SettingsScreen_Open
    {
        [HarmonyPostfix]
        internal static void Postfix(SettingsScreen __instance)
        {
            var profileButton = __instance.profileBtn.transform.parent.gameObject;

            var editPlayerData = profileButton.Duplicate();
            // thought Duplicate was supposed to do this but whatever :/
            editPlayerData.transform.SetParent(profileButton.transform.parent);
            editPlayerData.name = "EditPlayerData";
            
            var button = editPlayerData.GetComponentInChildren<Button>();
            button.SetOnClick(() => { ModGameMenu.Open<EditPlayerDataMenu>(); });
            
            
            var text = editPlayerData.GetComponentInChildren<NK_TextMeshProUGUI>();
            text.AutoLocalize = false;
            text.text = "Player Data";
        }
    }
}