using System;
using System.IO;
using System.Linq;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using EditPlayerData;
using HarmonyLib;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Unity.UI_New.Popups;
using Il2CppAssets.Scripts.Unity.UI_New.Settings;
using Il2CppAssets.Scripts.Utils;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(EditPlayerData.EditPlayerData), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]
namespace EditPlayerData;

public class EditPlayerData : BloonsTD6Mod
{

    public override void OnApplicationStart()
    {
        Directory.CreateDirectory("EditPlayerData");
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

            var button = panel.AddModHelperPanel(new Info("Edit Data Button"),
                null, RectTransform.Axis.Vertical, 10);
            button.LayoutGroup.childAlignment = TextAnchor.UpperCenter;
            button.AddPanel(new Info("Spacing", 200));
            
            var buttons = button.AddPanel(new Info("Buttons", 1700, 285), null, RectTransform.Axis.Horizontal, 100+(500-285));
            buttons.LayoutGroup.childAlignment = TextAnchor.UpperCenter;

            buttons.AddButton(new Info("Import", 285), VanillaSprites.EditBtn,
                new Action(() =>
                {
                    PopupScreen.instance.SafelyQueue(screen =>
                    {
                        var files = Directory
                            .EnumerateFiles(Path.Join(Directory.GetCurrentDirectory(), "EditPlayerData"))
                            .Select(Path.GetFileNameWithoutExtension)
                            .ToIl2CppList();
                        files.Sort();

                        if (files._size <= 0)
                        {
                            var okPopup = screen.ShowOkPopup(
                                "No savefiles found. Additional files can be added to:").WaitForCompletion().FindObject("Layout");

                            var okText = okPopup.AddModHelperComponent(ModHelperText.Create(new Info("Text",2000, 50),
                                Path.Join(Directory.GetCurrentDirectory(), "EditPlayerData"), 40));
                            okText.transform.MoveAfterSibling(okPopup.FindObject("Body").transform, true);
                            
                            var okSpacing = okPopup.AddModHelperPanel(new Info("Spacing", 130));
                            okSpacing.transform.MoveAfterSibling(okText, true);
                            return;
                        }
                        
                        ModHelperDropdown? dropdown = null;
                        var popup = screen.ShowPopup(PopupScreen.Placement.inGameCenter, "Import Settings",
                            "Select the savefile to load.",
                            new Action(() =>
                            {
                                try
                                {
                                    EditPlayerDataMenu.DeserializeAllSettings(Path.Join("EditPlayerData", dropdown!.Text.Text.text + ".json"));
                                    screen.ShowOkPopup("Settings successfully loaded from file.");
                                }
                                catch
                                {
                                    screen.ShowOkPopup(
                                        "An error occured while loading. Check the MelonLoader console for messages and ensure the file is in the proper format.");
                                    throw;
                                }
                            }), "Import", null, "Cancel",
                            Popup.TransitionAnim.Scale, PopupScreen.BackGround.Grey).WaitForCompletion().FindObject("Layout");
                        
                        var text = popup.AddModHelperComponent(ModHelperText.Create(new Info("Text",2000, 50),
                            Path.Join(Directory.GetCurrentDirectory(), "EditPlayerData"), 40));
                        text.transform.MoveAfterSibling(popup.FindObject("Body").transform, true);
                        
                        var spacing = popup.AddModHelperPanel(new Info("Spacing", 60));
                        spacing.transform.MoveAfterSibling(text, true);
                        
                        dropdown = popup.AddModHelperComponent(ModHelperDropdown.Create(new Info("FileDropdown", 790, 125),
                            files, 550, new Action<int>(_ => { }),
                            VanillaSprites.BlueInsertPanelRound, 60f));
                        dropdown.transform.MoveAfterSibling(spacing, true);
                        
                        spacing = popup.AddModHelperPanel(new Info("Spacing", 130));
                        spacing.transform.MoveAfterSibling(dropdown, true);
                    });
                })); 
            buttons.AddButton(new Info("Settings", 285), VanillaSprites.SettingsBtn,
                new Action(() => { ModGameMenu.Open<EditPlayerDataMenu>(); }));
            buttons.AddButton(new Info("Export", 285), VanillaSprites.ExitGameBtn,
                new Action(() =>
                {
                    PopupScreen.instance.SafelyQueue(screen =>
                    {
                        ModHelperInputField? input = null;
                        var popup = screen.ShowPopup(PopupScreen.Placement.inGameCenter, "Export Settings", "Input a title for this savefile.",
                            new Action(() =>
                            {
                                var title = input!.CurrentValue;

                                
                                FileStream? file = null;
                                try
                                {
                                    file = File.Open(Path.Join("EditPlayerData", title + ".json"), FileMode.Create);
                                    EditPlayerDataMenu.SerializeAllSettings(file);
                                    var popup = screen.ShowOkPopup("Settings successfully saved:").WaitForCompletion().FindObject("Layout");
                                    
                                    var text = popup.AddModHelperComponent(ModHelperText.Create(new Info("Text",2000, 50),
                                        Path.Join(Directory.GetCurrentDirectory(), "EditPlayerData", title+".json"), 40));
                                    text.transform.MoveAfterSibling(popup.FindObject("Body").transform, true);
                                    
                                    var spacing = popup.AddModHelperPanel(new Info("Spacing", 130));
                                    spacing.transform.MoveAfterSibling(text, true);
                                }
                                catch
                                {
                                    screen.ShowOkPopup(
                                        "An error occured while saving. Check the MelonLoader console for messages.");
                                    throw;
                                }
                                finally
                                {
                                    file?.Close();
                                }
                            }), "Export", null, "Cancel",
                            Popup.TransitionAnim.Scale, PopupScreen.BackGround.Grey).WaitForCompletion().FindObject("Layout");

                        input = popup.AddModHelperComponent(ModHelperInputField.Create(new Info("Input", 790, 125),
                            "playerdata", VanillaSprites.BlueInsertPanelRound, fontSize: 60));
                        input.transform.MoveAfterSibling(popup.FindObject("Body").transform, true);
                        
                        var spacing = popup.AddModHelperPanel(new Info("Spacing", 130));
                        spacing.transform.MoveAfterSibling(input, true);

                    });
                }));
            
            var texts = button.AddPanel(new Info("Texts", 2000, 100), null, RectTransform.Axis.Horizontal, 100);
            texts.LayoutGroup.childAlignment = TextAnchor.UpperCenter;
            texts.AddText(new Info("Text", 500, 100), "Import", 80).Text.alignment = TextAlignmentOptions.Center;
            texts.AddText(new Info("Text", 500, 100), "Player Data", 80).Text.alignment = TextAlignmentOptions.Center;
            texts.AddText(new Info("Text", 500, 100), "Export", 80).Text.alignment = TextAlignmentOptions.Center;
        }
    }
}