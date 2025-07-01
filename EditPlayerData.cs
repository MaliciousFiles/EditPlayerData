using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using EditPlayerData;
using EditPlayerData.UI;
using HarmonyLib;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Unity.UI_New.Popups;
using Il2CppAssets.Scripts.Unity.UI_New.Settings;
using Il2CppAssets.Scripts.Utils;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[assembly: MelonInfo(typeof(EditPlayerData.EditPlayerData), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]
[assembly: MelonOptionalDependencies("System.Windows.Forms")] // loaded manually
namespace EditPlayerData;

public class EditPlayerData : BloonsTD6Mod
{

    public override void OnApplicationStart()
    {
        var versions = Directory.GetDirectories(@"C:\Windows\Microsoft.NET\Framework64\").Where(f=>f.Split('\\')[^1][0] == 'v').ToList();
        versions.Sort();
        MelonAssembly.LoadMelonAssembly(versions[^1]+@"\System.Windows.Forms.dll", false);

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
                    var dialog = new OpenFileDialog();
                    dialog.Filter = "Json files (*.json,*.txt)|*.json;*.txt|All files (*.*)|*.*";
                    dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    dialog.FileName = "playerdata.json";
                    dialog.DefaultExt = "json";
                    dialog.Title = "Import From";
                    dialog.Multiselect = false;
                    dialog.CheckPathExists = true;
                    dialog.CheckFileExists = true;
                    if (dialog.ShowDialog() != DialogResult.OK) return;

                    try
                    {
                        EditPlayerDataMenu.DeserializeAllSettings(dialog.FileName);
                        PopupScreen.instance.SafelyQueue(screen =>
                        {
                            screen.ShowOkPopup("Settings successfully loaded from file.");
                        });
                    }
                    catch
                    {
                        PopupScreen.instance.SafelyQueue(screen =>
                        {
                            screen.ShowOkPopup(
                                "An error occured while loading. Check the MelonLoader console for messages and ensure the file is in the proper format.");
                        });

                        throw;
                    }
                })); 
            buttons.AddButton(new Info("Settings", 285), VanillaSprites.SettingsBtn,
                new Action(() => { ModGameMenu.Open<EditPlayerDataMenu>(); }));
            buttons.AddButton(new Info("Export", 285), VanillaSprites.ExitGameBtn,
                new Action(() =>
                {
                    var dialog = new SaveFileDialog();
                    dialog.Filter = "Json files (*.json,*.txt)|*.json;*.txt|All files (*.*)|*.*";
                    dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    dialog.FileName = "playerdata.json";
                    dialog.DefaultExt = "json";
                    dialog.OverwritePrompt = true;
                    dialog.Title = "Export As";
                    dialog.CheckPathExists = true;
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    
                    var file = File.Open(dialog.FileName, FileMode.Create);
                    try
                    {
                        EditPlayerDataMenu.SerializeAllSettings(file);
                        PopupScreen.instance.SafelyQueue(screen =>
                        {
                            screen.ShowOkPopup("Settings successfully saved to file.");
                        });
                    }
                    catch
                    {
                        PopupScreen.instance.SafelyQueue(screen =>
                        {
                            screen.ShowOkPopup("An error occured while saving. Check the MelonLoader console for messages.");
                        });

                        throw;
                    }
                    finally
                    {
                        file.Close();
                    }
                }));
            
            var texts = button.AddPanel(new Info("Texts", 2000, 100), null, RectTransform.Axis.Horizontal, 100);
            texts.LayoutGroup.childAlignment = TextAnchor.UpperCenter;
            texts.AddText(new Info("Text", 500, 100), "Import", 80).Text.alignment = TextAlignmentOptions.Center;
            texts.AddText(new Info("Text", 500, 100), "Player Data", 80).Text.alignment = TextAlignmentOptions.Center;
            texts.AddText(new Info("Text", 500, 100), "Export", 80).Text.alignment = TextAlignmentOptions.Center;
        }
    }
}