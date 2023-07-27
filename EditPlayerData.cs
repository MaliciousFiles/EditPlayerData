using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using EditPlayerData;
using EditPlayerData.UI;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.Settings;
using Il2CppAssets.Scripts.Utils;
using Il2CppSystem;
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
            var panel = __instance.gameObject.FindObject("Panel");

            var button = panel.AddModHelperPanel(new Info("Edit Data Button", 700),
                null, RectTransform.Axis.Vertical, 10);
            button.AddPanel(new Info("Spacing", 150));
            button.AddButton(new Info("Button", 300), VanillaSprites.SettingsBtn,
                new System.Action(() => { ModGameMenu.Open<EditPlayerDataMenu>(); }));
            button.AddText(new Info("Text", 700, 100), "Player Data", 80);

            // var profileButton = __instance.profileBtn.transform.parent.gameObject;
            // __instance.profileBtn.transform.parent.parent.gameObject.AddModHelperPanel(new Info("Spacing", 50));

            // var editPlayerData = profileButton.Duplicate();
            // thought Duplicate was supposed to do this but whatever :/
            // editPlayerData.transform.SetParent(profileButton.transform.parent);
            // editPlayerData.name = "EditPlayerData";

            // editPlayerData.GetComponentInChildren<Button>()
            // .SetOnClick();

            // editPlayerData.GetComponentInChildren<Image>().SetSprite(VanillaSprites.SettingsBtn);

            // var text = editPlayerData.GetComponentInChildren<NK_TextMeshProUGUI>();
            // text.AutoLocalize = false;
            // text.text = "Player Data";
        }
    }
}