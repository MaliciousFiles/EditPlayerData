using System;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Unity;
using Il2CppNinjaKiwi.Common;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;

namespace EditPlayerData.UI;

[RegisterTypeInIl2Cpp(false)]
internal class PlayerDataSettingDisplay : ModHelperPanel
{
    public PlayerDataSettingDisplay(IntPtr ptr) : base(ptr) { }

    public static PlayerDataSettingDisplay Generate(string name)
    {
        var panel = Create<PlayerDataSettingDisplay>(new Info(name, InfoPreset.Flex) {
                Height = 300,
                FlexWidth = 1,
                Pivot = new Vector2(0.5f, 1)
            }, null, RectTransform.Axis.Horizontal, 50);

        var mainPanel = panel.AddPanel(new Info("MainPanel", InfoPreset.Flex)
            {
                Height = 300
            },
            VanillaSprites.MainBGPanelBlue, RectTransform.Axis.Horizontal, 0, 50);

        mainPanel.AddPanel(new Info("IconPanel", 300));

        mainPanel.AddText(new Info("Title")
        {
            Height = 300,
            FlexWidth = 3
        }, "Name", 85, TextAlignmentOptions.MidlineLeft);


        mainPanel.AddPanel(new Info("Value")
        {
            Height = 300,
            FlexWidth = 4
        });
        mainPanel.AddPanel(new Info("Spacing", 100));
        
        panel.AddPanel(new Info("Edit", 225));
        panel.AddButton(new Info("Reset", 225), VanillaSprites.RedBtn, null)
            .AddImage(new Info("ResetIcon", 175), VanillaSprites.RestartIcon);

        return panel;
    }

    private void SetPanelContents(string panelName, ModHelperComponent contents)
    {
        var panel = GetDescendent<ModHelperPanel>(panelName);
        panel.transform.DestroyAllChildren();
        panel.Add(contents);
    }

    public void SetSetting(PlayerDataSetting setting)
    {
        setting.ReloadVisuals = () =>
        {
            SetSetting(setting);
        };
        
        GetDescendent<ModHelperText>("Title").SetText(setting.Name);
        SetPanelContents("IconPanel", setting.GetIcon());
        SetPanelContents("Value", setting.GetValueDisplay());
        SetPanelContents("Edit", setting.GetEditButton());
        GetDescendent<ModHelperButton>("Reset").Button.SetOnClick(() =>
        {
            if (!setting.IsUnlocked()) return;
            setting.ResetToDefault();
            Game.instance.playerService.Player.Save();
            SetSetting(setting);
        });
    }
}