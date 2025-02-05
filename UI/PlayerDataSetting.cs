using System;
using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using EditPlayerData.Utils;
using HarmonyLib;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Data.Knowledge.RelicKnowledge;
using Il2CppAssets.Scripts.Data.MapSets;
using Il2CppAssets.Scripts.Data.Store;
using Il2CppAssets.Scripts.Data.TrophyStore;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Models.TowerSets;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Player;
using Il2CppAssets.Scripts.Unity.UI_New.Popups;
using Il2CppAssets.Scripts.Utils;
using Il2CppInterop.Runtime;
using Il2CppNinjaKiwi.Common.ResourceUtils;
using Il2CppNinjaKiwi.Common;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// ReSharper disable AccessToModifiedClosure

namespace EditPlayerData.UI;

public abstract class PlayerDataSetting
{
    public readonly string Name;
    protected readonly string Icon;

    public Action? ReloadVisuals
    {
        set;
        protected get;
    }
    
    private bool _unlockable;
    private Func<bool>? _isLocked;
    private Action? _unlock;

    protected PlayerDataSetting(string name, string icon)
    {
        Name = name;
        Icon = icon;
    }
    
    public ModHelperButton GetEditButton()
    {
        if (!_unlockable || !_isLocked!())
        {
            return ModHelperButton.Create(new Info("Edit", InfoPreset.FillParent), VanillaSprites.EditBtn,
                new Action(() =>
                {
                    PopupScreen.instance.SafelyQueue(ShowEditValuePopup);
                }));
        }
        
        var button = ModHelperButton.Create(new Info("Edit", InfoPreset.FillParent), VanillaSprites.GreenBtn,
            new Action(() =>
            {
                Unlock();
                ReloadVisuals?.Invoke();
            }));
        button.AddImage(new Info("UnlockIcon", 120), VanillaSprites.LockIconOpen);

        return button;
    }

    /**
     * Returns whether it is unlocked, or true if it is not unlockable
     */
    public bool IsUnlocked()
    {
        return !_unlockable || !_isLocked!();
    }

    public PlayerDataSetting Unlockable(Func<bool> isLocked, Action unlock)
    {
        _unlockable = true;
        _isLocked = isLocked;
        _unlock = unlock;

        return this;
    }

    public void Unlock()
    {
        if (!IsUnlocked()) _unlock!();
    }

    public ModHelperComponent GetValueDisplay()
    {
        if (IsUnlocked()) return GetValue();
        
        var panel = ModHelperPanel.Create(new Info("Lock", InfoPreset.FillParent),
            layoutAxis: RectTransform.Axis.Horizontal);
        panel.LayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            
        panel.AddImage(new Info("LockedIcon", 120), VanillaSprites.LockIcon);

        return panel;

    }

    public virtual ModHelperImage GetIcon()
    {
        return ModHelperImage.Create(new Info("Icon")
        {
            X = -50, Size = 350
        }, Icon);
    }

    protected abstract ModHelperComponent GetValue();
    protected abstract void ShowEditValuePopup(PopupScreen screen);
    public abstract void ResetToDefault();
}

public abstract class TypedPlayerDataSetting<T> : PlayerDataSetting
{
    protected readonly Func<T> Getter;
    protected readonly Action<T> Setter;
    private readonly T _default;

    protected TypedPlayerDataSetting(string name, string icon, T def, Func<T> getter, Action<T> setter) :
        base(name, icon)
    {
        _default = def;
        Getter = getter;
        Setter = setter;
    }

    protected override ModHelperComponent GetValue()
    {
        var text = ModHelperText.Create(new Info("ValueText", InfoPreset.FillParent),
            Getter()+"", 70);
        text.Text.alignment = TextAlignmentOptions.Right;

        return text;
    }

    public override void ResetToDefault()
    {
        Setter(_default);
    }
}

public class NumberPlayerDataSetting : TypedPlayerDataSetting<int>
{
    public NumberPlayerDataSetting(string name, string icon, int def, Func<int> getter, Action<int> setter) :
        base(name, icon, def, getter, setter) { }

    private static bool _isShown;
    protected override void ShowEditValuePopup(PopupScreen screen)
    {
        screen.ShowSetValuePopup("Edit Value", "The new value to set.",
            new Action<int>(n =>
            {
                Setter(n);
                ReloadVisuals?.Invoke();
            }), Getter());
        _isShown = true;
    }

    [HarmonyPatch(typeof(Popup), nameof(Popup.ShowPopup))]
    // ReSharper disable once InconsistentNaming
    internal class Popup_ShowPopup
    {
        [HarmonyPrefix]
        internal static void Prefix()
        {
            if (!_isShown) return;
            _isShown = false;


            PopupScreen.instance.GetTMP_InputField().characterLimit = 9;
        }
    }
}

public class BoolPlayerDataSetting : TypedPlayerDataSetting<bool>
{
    public BoolPlayerDataSetting(string name, string icon, bool def, Func<bool> getter, Action<bool> setter) :
        base(name, icon, def, getter, setter) { }
    
    protected override void ShowEditValuePopup(PopupScreen screen)
    {
        var value = Getter();
                        
        var popupBody = screen.ShowPopup(PopupScreen.Placement.inGameCenter, "Edit Value", "The new value to set.",
            new Action(() =>
            {
                Setter(value);
                ReloadVisuals?.Invoke();
            }), "Ok", new Action(() => {}), "Cancel",
            Popup.TransitionAnim.Scale, PopupScreen.BackGround.Grey).WaitForCompletion().FindObject("Body");
                        
        popupBody.AddModHelperComponent(
            ModHelperCheckbox.Create(new Info("Checkbox", 0, -275, 200),
                value, VanillaSprites.SmallSquareDarkInner,
                new Action<bool>(b => value = b)));

        var spacer = popupBody.transform.parent.gameObject.AddModHelperPanel(new Info("Spacer", 200));
        spacer.transform.MoveAfterSibling(popupBody.transform, true);
    }
}

public class PurchasePlayerDataSetting : BoolPlayerDataSetting
{
    private string id;

    public PurchasePlayerDataSetting(string name, string icon, string id) : base(
        name, icon, false,
        () => Game.Player.Data.purchase.HasMadeOneTimePurchase(id),
        t =>
        {
            if (t) Game.Player.Data.purchase.AddOneTimePurchaseItem(id);
            else Game.Player.Data.purchase.RemoveOneTimePurchaseItem(id);
        })
    {
        this.id = id;
    }

    public PurchasePlayerDataSetting(string name, string icon, string id, Func<bool> getter, Action<bool> setter) : base(
        name, icon, false, getter, setter)
    {
        this.id = id;
    }

    protected override void ShowEditValuePopup(PopupScreen screen)
    {
        if (!Getter())
        {
            screen.ShowPopup(PopupScreen.Placement.inGameCenter, "Are you Sure?",
                "Ninja Kiwi is an incredible company, making great games at low prices. Please consider supporting them if at all possible.",
                new Action(() =>
                {
                    screen.ShowStorePopup(GameData.Instance.storeItems.GetProduct(id),
                        new Action(() => ReloadVisuals?.Invoke()));
                }), "I will!",
                new Action(() => base.ShowEditValuePopup(screen)), "I can't :(",
                Popup.TransitionAnim.Scale, PopupScreen.BackGround.GreyNonDismissable);
        }
        else base.ShowEditValuePopup(screen);
    }
};

public class MapPlayerDataSetting : PlayerDataSetting
{
    private static readonly Dictionary<string, string[]> Difficulties = new()
    {
        { "Easy", new []{ "Standard", "PrimaryOnly", "Deflation" } },
        { "Medium", new []{ "Standard", "MilitaryOnly", "Reverse", "Apopalypse" } },
        { "Hard", new []{ "Standard", "MagicOnly", "AlternateBloonsRounds", "DoubleMoabHealth", "HalfCash", "Impoppable", "Clicks" } }
    };

    private const float MapIconWidthToHeightRatio = 532f / 826f;

    private readonly MapDetails _details;
    private readonly MapInfo _map;
    private readonly bool _coop;

    public Action? ReloadAllVisuals
    {
        set;
        private get;
    }
    
    public MapPlayerDataSetting(MapDetails details, MapInfo map, bool coop) :
        base(LocalizationManager.Instance.Format(details.id), details.mapSprite.AssetGUID)
    {
        _details = details;
        _map = map;
        _coop = coop;
    }

    public override ModHelperImage GetIcon()
    {
        var image = ModHelperImage.Create(new Info("IconBorder")
        {
            X = -50, Width = 350, Height = 350 * MapIconWidthToHeightRatio
        }, VanillaSprites.MainBgPanel);
        
        var mask = image.AddImage(new Info("Mask")
        {
            Width = 295, Height = 295 * MapIconWidthToHeightRatio
        }, VanillaSprites.MainBgPanel);
        mask.gameObject.AddComponent<Mask>();

        mask.AddImage(new Info("Icon")
        {
            Width = 325, Height = 325 * MapIconWidthToHeightRatio
        }, Icon);

        return image;
    }

    protected override void ShowEditValuePopup(PopupScreen screen)
    {
        ModHelperTable? settings = null;
        Action<char>? tabListener = null;

        var affectedValue = 0;

        var popup = screen.ShowPopup(PopupScreen.Placement.inGameCenter, "Edit Value", 
            "Edit each game-mode's settings",
            new Action(() =>
            {
                for (var i = 1; i < settings!.RowCount; i++)
                {
                    var row = settings.GetRow(i);
                    
                    var difficulty = row.name.Split("/")[0];
                    var mode = Difficulties[difficulty][int.Parse(row.name.Split("/")[1])];

                    var wins = int.Parse(row.GetDescendent<ModHelperInputField>("WinCount").CurrentValue[..^1]);
                    var noExit = row.GetDescendent<ModHelperCheckbox>("NoExit").CurrentValue;


                    foreach (var mapMode in (affectedValue switch
                             {
                                 0 => new[] { _details }.ToList(),
                                 1 => GameData.Instance.mapSet.GetStandardMapsForDifficulty(_details.difficulty).ToList(),
                                 2 => GameData.Instance.mapSet.StandardMaps.ToList(),
                                 _ => new List<MapDetails>()
                             }).Select(map =>
                                 Game.Player.Data.mapInfo.GetMap(map.id).GetOrCreateDifficulty(difficulty)
                                     .GetOrCreateMode(mode, _coop))) 
                    {
                        mapMode.timesCompleted = wins;
                        mapMode.completedWithoutLoadingSave = noExit;
                    }
                }

                ReloadAllVisuals();
            }), "Ok", new Action(() => { Keyboard.current.remove_onTextInput(tabListener!); }), "Cancel",
            Popup.TransitionAnim.Scale, PopupScreen.BackGround.Grey).WaitForCompletion().FindObject("Layout");
        
        settings = popup.AddModHelperComponent(ModHelperTable.Create(
            new Info("MapSettings", 1750, 700), 3,
            VanillaSprites.InsertPanelWhiteRound, 80, new[] {3, 2, 2},
            60, 150, new RectOffset { top = 22, left = 35, right = 35 }));

        settings.Background.color = new Color(0.29f, 0.51f, 0.81f);
        settings.transform.MoveAfterSibling(popup.FindObject("Body").transform, true);

        settings.SetValue(0, 1, ModHelperText.Create(new Info("Text"), "Wins", 75));
        settings.SetValue(0, 2, ModHelperText.Create(new Info("Text"), "Exitless", 75));

        ModHelperInputField? selectedInput = null;
        tabListener = c =>
        {
            if (c != 9) return;

            try
            {
                var idx = (selectedInput == null ? 0 : selectedInput.GetComponentInParent<ModHelperTableRow>().Row) +
                          (Keyboard.current.shiftKey.isPressed ? -1 : 1);
                if (idx < 1 || idx >= settings.RowCount) return;

                settings.GetRow(idx).GetComponentInChildren<ModHelperInputField>()?.InputField.Select();
                settings.ScrollTo(idx);
            }
            catch (Il2CppException _) // popup probably closed, remove listener
            {
                Keyboard.current.remove_onTextInput(tabListener);
            }
        };
        Keyboard.current.add_onTextInput(tabListener);
        
        var difficulties = new[] { "Bronze", "Silver", "Gold", "Impoppable" };
        var row = 1;
        
        for (var i = 0; i < difficulties.Length; i++)
        {
            var difficulty = difficulties[i];
            var chimps = difficulty == "Impoppable";
            for (var k = 0; k < (chimps ? 2 : i+3); k++)
            {
                var difficultyEntry = Difficulties.ToArray()[i - (chimps ? 1 : 0)];

                var modeIdx = k + (chimps ? 5 : 0);
                var name = $"{difficultyEntry.Key}/{modeIdx}";
                var modeName = k == 0 && !chimps ? difficultyEntry.Key :
                        $"Mode {difficultyEntry.Value[modeIdx]}";

                var modeInfo = _map.GetOrCreateDifficulty(difficultyEntry.Key)
                    .GetOrCreateMode(difficultyEntry.Value[modeIdx], _coop);

                var medalName = $"{difficulty}{(k > 0 ? chimps ? modeInfo.completedWithoutLoadingSave ? "Hematite" : "Ruby" : $"0{k}" : "")}";

                var medal = ModHelperPanel.Create(new Info("MedalAndName", InfoPreset.Flex),
                    layoutAxis: RectTransform.Axis.Horizontal, spacing: 5);
                medal.AddText(new Info("Name", InfoPreset.Flex),
                    LocalizationManager.Instance.Format(modeName), k == 0 ? 65 : 50);
                var medalImage = medal.AddImage(new Info("Medal", k == 0 ? 135 : 125), $"MapMedals[Medal{medalName}]");

                var input = ModHelperInputField.Create(new Info("WinCount"),
                    modeInfo.timesCompleted.ToString(), VanillaSprites.BlueInsertPanelRound, null,
                    60, TMP_InputField.CharacterValidation.Digit);
                input.InputField.onSelect.AddListener(new Action<string>(_ =>
                {
                    selectedInput = input;
                }));
                input.InputField.onDeselect.AddListener(new Action<string>(_ =>
                {
                    selectedInput = null;
                }));
                
                settings.SetValue(row, 0, medal);
                settings.SetValue(row, 1, input);

                var isClicks = k > 0 && chimps;
                settings.SetValue(row, 2, ModHelperCheckbox.Create(new Info("NoExit", 100),
                    modeInfo.completedWithoutLoadingSave, VanillaSprites.SmallSquareDarkInner,
                    new Action<bool>(b =>
                    {
                        if (isClicks)
                        {
                            medalImage.Image.SetSprite($"MapMedals[MedalImpoppable{(b ? "Hematite" : "Ruby")}]");
                        }
                    })));

                settings.GetRow(row).name = name;
                
                if (i != 0 && k == 0)
                {
                    var spacing = ModHelperPanel.Create(new Info("Spacing") { Height = 20 });
                    
                    var line = spacing.AddPanel(new Info("HorizontalLine",
                        settings.RectTransform.rect.width - 200, 5));
                    line.AddComponent<Image>().color = new Color(0.39f, 0.61f, 0.91f);
                    
                    spacing.SetParent(settings.GetRow(row).parent);
                    spacing.transform.SetSiblingIndex(
                            settings.GetRow(row).transform.GetSiblingIndex());
                }

                row++;
            }
        }
        
        var affected = popup.AddModHelperPanel(new Info("Affected", 1500, 125),
            null, RectTransform.Axis.Horizontal);
        affected.AddText(new Info("Label", 575), "Affected Map(s): ", 60);
        affected.AddDropdown(new Info("Options", 790, 125),
            new[] { "Current Map", $"All {_details.difficulty} Maps", "All Maps" }.ToIl2CppList(), 400,
            new Action<int>(value =>
            {
                affectedValue = value;
            }), VanillaSprites.BlueInsertPanelRound, 50);
        affected.transform.MoveAfterSibling(settings.transform, true);

        popup.AddModHelperPanel(new Info("Spacing", 120)).
            transform.MoveAfterSibling(settings.transform, true);
        
        popup.AddModHelperPanel(new Info("Spacing", 90)).
            transform.MoveAfterSibling(affected.transform, true);
        
        var text = popup.AddModHelperComponent(ModHelperText.Create(new Info("HintText", settings.RectTransform.rect.width, 100),
            "Press TAB to navigate quickly!", 50));
        text.Text.font = Fonts.Btd6FontBody;
        text.transform.MoveAfterSibling(popup.FindObject("Body").transform, true);
    }
    
    protected override ModHelperComponent GetValue()
    {
        var panel = ModHelperPanel.Create(new Info("Medals", InfoPreset.FillParent),
            null, RectTransform.Axis.Horizontal, 215);
        panel.LayoutGroup.childAlignment = TextAnchor.MiddleRight;

        var easy = panel.AddPanel(new Info("Easy/Bronze"));
        easy.Add(CreateMedal("Standard"));
        easy.Add(CreateMedal("PrimaryOnly", -70.5f, -60.1f));
        easy.Add(CreateMedal("Deflation", 56.8f, -52));

        var medium = panel.AddPanel(new Info("Medium/Silver")
        {
            Y = 20
        });
        medium.Add(CreateMedal("Standard"));
        medium.Add(CreateMedal("MilitaryOnly", -57, -69));
        medium.Add(CreateMedal("Reverse", 60, -56));
        medium.Add(CreateMedal("Apopalypse", 0, -96));

        var hard = panel.AddPanel(new Info("Hard/Gold"));
        hard.Add(CreateMedal("Standard"));
        hard.Add(CreateMedal("MagicOnly", -75, -1));
        hard.Add(CreateMedal("AlternateBloonsRounds", 73, 5));
        hard.Add(CreateMedal("DoubleMoabHealth", -45, -73));
        hard.Add(CreateMedal("HalfCash", 48, -65));

        var impoppable = panel.AddPanel(new Info("Hard/Impoppable")
        {
            Y = 15
        });
        impoppable.Add(CreateMedal("Impoppable"));
        impoppable.Add(CreateMedal("Clicks", 55, -78));
        // TODO: medium and impoppable medals aren't higher, despite their `Info`s

        FillInMedals(panel);

        return panel;
    }

    public override void ResetToDefault()
    {
        if (_map.difficult == null) return;
        
        foreach (var difficulty in _map.difficult)
        {
            foreach (var mode in _coop ? difficulty.Value.coopModes : difficulty.Value.modes)
            {
                mode.Value.timesCompleted = 0;
                mode.Value.completedWithoutLoadingSave = false;
            }
        }
    }
    
    private MapModeInfo? GetModeInfo(Component medal)
    {
        return _map.GetDifficulty(medal.transform.parent.name.Split("/")[0])?.
            GetMode(medal.name, _coop);
    }

    private void FillInMedals(Component medals)
    {
        foreach (var medal in medals.GetComponentsInChildren<Image>())
        {
            var mode = GetModeInfo(medal);
            if (mode == null || mode.timesCompleted == 0) continue;

            var medalIdx = medal.transform.GetSiblingIndex();
            var medalSuffix = medal.transform.parent.name.Split("/")[1] +
                (medal.name == "Clicks" ? // Chimps
                    mode.completedWithoutLoadingSave ? "Hematite" : "Ruby" :
                    medalIdx > 0 ? "0" + medalIdx : "");

            var newSize = medalIdx == 0 ? 180 : 120;
            medal.rectTransform.sizeDelta = new Vector2(newSize, newSize);

            medal.color = Color.white; // reset the colors from the stars
            medal.SetSprite($"MapMedals[Medal{medalSuffix}]");
        }
    }
    
    private static ModHelperImage CreateMedal(string name, float xMod = 0, float yMod = 0)
    {
        var primary = xMod == 0 && yMod == 0;
        var img =  ModHelperImage.Create(new Info(name, primary ? 100 : 40) {
            X = xMod, Y = yMod
        }, primary ? VanillaSprites.MedalEmpty : VanillaSprites.GlowyStarUi);
    
        img.Image.color = new Color(0.6887f, 0.5375f, 0.3476f);
        
        // TODO: maybe make the stars appear behind the main medal?
        
        return img;
    }
}

public class RankPlayerDataSetting : PlayerDataSetting
{
    private readonly Func<Btd6Player> _getPlayer;
    
    public RankPlayerDataSetting(Func<Btd6Player> getPlayer) : base("Rank", "")
    {
        _getPlayer = getPlayer;
    }

    protected override ModHelperComponent GetValue()
    {
        var panel = ModHelperPanel.Create(new Info("XP", InfoPreset.FillParent),
            null, RectTransform.Axis.Horizontal, 50);
        panel.LayoutGroup.childAlignment = TextAnchor.MiddleRight;

        panel.AddImage(new Info("PlayerRank", 250), VanillaSprites.PlayerXPIcon)
            .AddText(new Info("PlayerRankText", 250), _getPlayer().Data.rank.ValueInt.ToString(), 50);
        
        panel.AddImage(new Info("VeteranRank", 250), VanillaSprites.VeteranXPIcon)
            .AddText(new Info("VeteranRankText", 250), _getPlayer().Data.rank.ValueInt == GameData.Instance.rankInfo.GetMaxRank() ? _getPlayer().Data.veteranRank.ValueInt.ToString() : "0", 50);

        return panel;
    }

    public override ModHelperImage GetIcon()
    {
        return ModHelperImage.Create(new Info("Icon")
        {
            X = -50, Size = 350
        }, _getPlayer().Data.rank.ValueInt == GameData.Instance.rankInfo.GetMaxRank() ? VanillaSprites.VeteranXPIcon : VanillaSprites.PlayerXPIcon);        
    }

    protected override void ShowEditValuePopup(PopupScreen screen)
    {
        screen.ShowSetValuePopup("Edit Value", "The new value to set.\nRanks above 155 will be converted to veteran ranks.",
            new Action<int>(n =>
            {
                _getPlayer().Data.seenVeteranRankInfo = true;
                
                var rankInfo = GameData.Instance.rankInfo;

                var rank = Math.Min(n, rankInfo.GetMaxRank());
                var veteranRank = Math.Max(n - rankInfo.GetMaxRank(), 0);

                _getPlayer().Data.rank.Value = rank;
                _getPlayer().Data.veteranRank.Value = rank == rankInfo.GetMaxRank() ? veteranRank + 1 : 0;
                
                _getPlayer().Data.xp.Value = rankInfo.GetRankInfo(rank-1).totalXpNeeded;
                _getPlayer().Data.veteranXp.Value = (long) veteranRank * rankInfo.xpNeededPerVeteranRank;
                
                ReloadVisuals?.Invoke();
            }), _getPlayer().Data.rank.ValueInt + (_getPlayer().Data.rank.ValueInt == GameData.Instance.rankInfo.GetMaxRank() ? _getPlayer().Data.veteranRank.ValueInt-1 : 0));
    }

    public override void ResetToDefault()
    {
        _getPlayer().Data.xp.Value = _getPlayer().Data.veteranXp.Value = 0;
        _getPlayer().CheckAndCorrectLevelBasedOnPlayerXp();
    }
}

public class TowerPlayerDataSetting : NumberPlayerDataSetting
{
    private readonly TowerDetailsModel _tower;
    private readonly Func<Btd6Player> _getPlayer;
    
    public TowerPlayerDataSetting(TowerDetailsModel tower, Func<Btd6Player> getPlayer)
        : base(LocalizationManager.Instance.Format(tower.towerId),
            tower.GetTower().portrait.GetGUID(), 0,
            () => getPlayer().Data.towerXp.ContainsKey(tower.towerId) ?
                getPlayer().Data.towerXp[tower.towerId].ValueInt : 0,
            t =>
            {
                var data = getPlayer().Data;
                if (!data.towerXp.ContainsKey(tower.towerId))
                {
                    data.towerXp[tower.towerId] = new KonFuze_NoShuffle(t);
                }
                else
                {
                    data.towerXp[tower.towerId].Value = t;
                }
            })
    {
        _tower = tower;
        _getPlayer = getPlayer;
    }

    private IEnumerable<string> GetAllUpgrades()
    {
        var model = Game.instance.model;

        var upgrades = model.GetTower(_tower.towerId, pathOneTier: 5).appliedUpgrades
            .Concat(model.GetTower(_tower.towerId, pathTwoTier: 5).appliedUpgrades)
            .Concat(model.GetTower(_tower.towerId, pathThreeTier: 5).appliedUpgrades);
        
        var paragon = Game.instance.model.GetParagonUpgradeForTowerId(_tower.towerId);
        return paragon != null ? upgrades.Append(paragon.name) : upgrades;
    }

    protected override ModHelperComponent GetValue()
    {
        var panel = ModHelperPanel.Create(new Info("Value", InfoPreset.FillParent),
            null, RectTransform.Axis.Horizontal, 100);
        panel.LayoutGroup.childAlignment = TextAnchor.MiddleRight;

        if (GetAllUpgrades().Any(u => !_getPlayer().HasUpgrade(u)))
        {
            panel.AddButton(new Info("UnlockUpgrades", 335, 160), VanillaSprites.GreenBtnLong,
                new Action(() =>
                {
                    foreach (var upgrade in GetAllUpgrades())
                    {
                        _getPlayer().Data.acquiredUpgrades.Add(upgrade);
                    }

                    ReloadVisuals?.Invoke();
                })).AddText(new Info("Text", 300, 100), "Unlock All Upgrades", 40);
        }
        else
        {
            panel.AddPanel(new Info("Spacing", 335));
        }
        
        var text = base.GetValue();
        text.SetInfo(new Info("ValueText", 320));
        panel.Add(text);

        return panel;
    }
}

public class InstaMonkeyPlayerDataSetting : PlayerDataSetting
{
    private readonly TowerDetailsModel _tower;
    private readonly Func<Btd6Player> _getPlayer;
    
    public InstaMonkeyPlayerDataSetting(TowerDetailsModel tower, Func<Btd6Player> getPlayer) :
        base(LocalizationManager.Instance.Format(tower.towerId),
            tower.GetTower().instaIcon.GetGUID())
    {
        _tower = tower;
        _getPlayer = getPlayer;
    }

    protected override ModHelperComponent GetValue()
    {
        var panel = ModHelperPanel.Create(new Info("Value", InfoPreset.FillParent),
            null, RectTransform.Axis.Horizontal, 25);
        panel.LayoutGroup.childAlignment = TextAnchor.MiddleRight;

        panel.AddButton(new Info("AddAll", 290, 140), VanillaSprites.GreenBtnLong,
            new Action(() =>
            {
                PopupScreen.instance.ShowSetValuePopup("Amount to Add", "Input the number of instas to add of each type.",
                    new Action<int>(n =>
                    {
                        var tierSet = new HashSet<int[]>(new TowerTiersEqualityComparer());
                
                        for (var mainPath = 0; mainPath < 3; mainPath++)
                        {
                            for (var mainPathTier = 0; mainPathTier <= 5; mainPathTier++)
                            {
                        
                                for (var crossPath = 0; crossPath < 3; crossPath++)
                                {
                                    for (var crossPathTier = 0; crossPathTier <= 2; crossPathTier++)
                                    {
                                        var tiers = new[] { 0, 0, 0 };
                                        tiers[crossPath] = crossPathTier;
                                        tiers[mainPath] = mainPathTier;

                                        tierSet.Add(tiers);
                                    }
                                }
                            }
                        }
                
                        foreach (var tiers in tierSet)
                        {
                            _getPlayer().GetInstaTower(_tower.towerId, tiers).Quantity += n;                    
                        }

                        ReloadVisuals?.Invoke();
                    }), 1);
            })).AddText(new Info("Text", 300, 100), "Add All", 45);

        
        panel.AddText(new Info("CountLabel") { Flex = 3 }, 
                "Count:", 60)
            .Text.alignment = TextAlignmentOptions.Right;
        panel.AddText(new Info("CountValue") { Flex = 3 },
            _getPlayer().GetInstaTowerGroupQuanity(_tower.towerId).ToString(), 75)
            .Text.alignment = TextAlignmentOptions.Left;
        
        panel.AddText(new Info("CollectionLabel") { Flex = 5 },
                "Collection:", 60)
            .Text.alignment = TextAlignmentOptions.Right;
        panel.AddText(new Info("CollectionValue") { Flex = 4 },
                $"{_getPlayer().GetInstaTowers(_tower.towerId).Count} / 64", 75)
            .Text.alignment = TextAlignmentOptions.Left;
        
        return panel;
    }

    protected override void ShowEditValuePopup(PopupScreen screen)
    {
        var instas = _getPlayer().Data.instaTowers[_tower.towerId];
        var oldInstas = instas.ToList().Select(t =>
            InstaTowerModel.FromInt32(InstaTowerModel.ToInt32(t)));
        
        var popup = screen.ShowPopup(PopupScreen.Placement.inGameCenter, "Edit Value", "Adjust the tiers of the various\npaths below, then set the quantity.",
            new Action(() =>
            {
                // I have to finagle this bc I don't know how to instantiate Il2Cpp predicates :/
                var towers = instas.ToList();
                towers.RemoveAll(t => t.Quantity == 0);
                _getPlayer().Data.instaTowers[_tower.towerId] = towers.ToIl2CppList();
                
                ReloadVisuals?.Invoke();
            }), "Ok", new Action(() =>
            {
                _getPlayer().Data.instaTowers[_tower.towerId] = oldInstas.ToIl2CppList();
            }), "Cancel",
            Popup.TransitionAnim.Scale, PopupScreen.BackGround.Grey).WaitForCompletion().FindObject("Layout");

        var currentTiers = new[] {0, 0, 0};

        var sprite = popup.AddModHelperComponent(ModHelperImage.Create(new Info("Sprite", 300),
            ""));
        sprite.transform.MoveAfterSibling(popup.transform.FindChild("Body"), true);

        var tiersPanel = popup.AddModHelperPanel(new Info("Tiers", 1750, 200),
            null, RectTransform.Axis.Horizontal, 75);
        tiersPanel.transform.MoveAfterSibling(sprite.transform, true);

        var input = popup.AddModHelperComponent(ModHelperInputField.Create(
            new Info("Input", 1250, 150), "", VanillaSprites.BlueInsertPanelRound,
            new Action<string>(s =>
            {
                _getPlayer().GetInstaTower(_tower.towerId, currentTiers).Quantity =
                    s == "" ? 0 : int.Parse(s);
            }), 80, TMP_InputField.CharacterValidation.Digit));
        input.transform.MoveAfterSibling(tiersPanel.transform, true);
        
        popup.AddModHelperPanel(new Info("Spacing", 175)).
            transform.MoveAfterSibling(input.transform, true);

        void UpdateArrow(ModHelperButton arrow, bool enabled)
        {
            arrow.Image.color = enabled ? new Color(1, 1, 1) :
                new Color(0.6431f, 0.7255f, 0.8235f);
            arrow.Button.interactable = enabled;
        }
        
        for (var i = 0; i < 3; i++)
        {
            var tierIdx = i; // has to not change for lambdas

            Action? update = null;
            
            var tier = tiersPanel.AddPanel(new Info($"Tier{i+1}", InfoPreset.Flex), null,
                RectTransform.Axis.Horizontal, 15);
            tier.LayoutGroup.childAlignment = TextAnchor.UpperCenter;
            
            tier.AddButton(new Info("Less", 125), VanillaSprites.PrevArrow,
                new Action(() =>
                {
                    currentTiers[tierIdx]--;
                    update!();
                }));
            var number = tier.AddText(new Info("Number", 125), "0", 55);
            tier.AddButton(new Info("More", 125), VanillaSprites.NextArrow,
                new Action(() =>
                {
                    currentTiers[tierIdx]++;
                    update!();
                }));

            update = () =>
            {
                input.InputField.Select();
                
                number.SetText(currentTiers[tierIdx].ToString());
                input.SetText(_getPlayer().GetInstaTower(_tower.towerId, currentTiers).Quantity.ToString());
                sprite.Image.SetSprite(Game.instance.model
                    .GetTower(_tower.towerId, currentTiers[0], currentTiers[1], currentTiers[2])
                    .instaIcon.AssetGUID);

                for (var j = 0; j < tiersPanel.transform.childCount; j++)
                {
                    var tempTiers = (int[]) currentTiers.Clone();
                    
                    tempTiers[j] = currentTiers[j]+1;
                    UpdateArrow(tiersPanel.transform.GetChild(j).FindChild("More").GetComponent<ModHelperButton>(),
                        Game.instance.model.GetTower(_tower.towerId, tempTiers[0], tempTiers[1], tempTiers[2]) != null);
                    
                    tempTiers[j] = currentTiers[j]-1;
                    UpdateArrow(tiersPanel.transform.GetChild(j).FindChild("Less").GetComponent<ModHelperButton>(),
                        Game.instance.model.GetTower(_tower.towerId, tempTiers[0], tempTiers[1], tempTiers[2]) != null);
                }
            };

            update();
        }
    }

    public override void ResetToDefault()
    {
        _getPlayer().Data.instaTowers[_tower.towerId]?.Clear();
    }
    
    public override ModHelperImage GetIcon()
    {
        var image = base.GetIcon();
        var info = image.initialInfo;
        image.SetInfo(new Info(info.Name)
        {
            X = info.X, Y = info.Y,
            Size = 300
        });

        return image;
    }
}