using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using EditPlayerData.UI;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Data.Boss;
using Il2CppAssets.Scripts.Data.Feats;
using Il2CppAssets.Scripts.Data.Legends;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Artifacts;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Models.Store;
using Il2CppAssets.Scripts.Models.Store.Loot;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Menu;
using Il2CppAssets.Scripts.Unity.Player;
using Il2CppAssets.Scripts.Unity.UI_New.Achievements;
using Il2CppAssets.Scripts.Unity.UI_New.ChallengeEditor;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.Popups;
using Il2CppAssets.Scripts.Utils;
using Il2CppInterop.Runtime;
using Il2CppNinjaKiwi.Common;
using Il2CppNinjaKiwi.Localization;
using Il2CppSystem.Linq;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;
using Action = System.Action;
using Enum = System.Enum;
using Object = Il2CppSystem.Object;

namespace EditPlayerData;

public class EditPlayerDataMenu : ModGameMenu<ContentBrowser>
{
    private static readonly Dictionary<string, List<PlayerDataSetting>> Settings = new()
    {
        {
            "General", new List<PlayerDataSetting>
            {
                new PurchasePlayerDataSetting("Unlocked Double Cash", VanillaSprites.DoubleCashModeShop, "btd6_doublecashmode", LootFrom.iap),
                new PurchasePlayerDataSetting("Unlocked Fast Track", VanillaSprites.FastTrackModeIcon,
                    "btd6_fasttrackpack",
                    () => GetPlayer().Data.unlockedFastTrack,
                    t => GetPlayer().Data.unlockedFastTrack = t),
                new PurchasePlayerDataSetting("Unlocked Rogue Legends", VanillaSprites.LegendRogueShop, "btd6_legendsrogue", LootFrom.legendFeat),
                new PurchasePlayerDataSetting("Unlocked Frontier Legends", VanillaSprites.LegendFrontierShop, "btd6_legendsfrontier", LootFrom.legendFeat),
                new PurchasePlayerDataSetting("Unlocked Map Editor", VanillaSprites.MapEditorBtn, "btd6_mapeditorsupporter_new", LootFrom.iap),
                new KonFuzePlayerDataSetting("Monkey Money", VanillaSprites.MonkeyMoneyShop, 0,
                    () => GetPlayer().Data.monkeyMoney),
                new MonkeyKnowledgePlayerDataSetting("Monkey Knowledge", VanillaSprites.KnowledgeIcon, 0,
                    () => GetPlayer().Data.knowledgePoints),
                new RankPlayerDataSetting(GetPlayer),
                
                new NumberPlayerDataSetting("Trophies", VanillaSprites.TrophyIcon, 0,
                    () => GetPlayer().Data.trophies.ValueInt,
                    t => GetPlayer().GainTrophies(t - GetPlayer().Data.trophies.ValueInt, "")),
                new NumberPlayerDataSetting("Rogue XP", VanillaSprites.RogueXpShopIconLarge, 0,
                    () => GetPlayer().Data.legendsData.rogueLegendXp, t => GetPlayer().Data.legendsData.rogueLegendXp = t),
                new BoolPlayerDataSetting("Unlocked Big Bloons", VanillaSprites.BigBloonModeIcon, false,
                    () => GetPlayer().Data.unlockedBigBloons, t => GetPlayer().Data.unlockedBigBloons = t),
                new BoolPlayerDataSetting("Unlocked Small Bloons", VanillaSprites.SmallBloonModeIcon, false,
                    () => GetPlayer().Data.unlockedSmallBloons, t => GetPlayer().Data.unlockedSmallBloons = t),
                new BoolPlayerDataSetting("Unlocked Small Bosses", VanillaSprites.SmallBossModeIcon, false,
                    () => GetPlayer().Data.unlockedSmallBosses, t => GetPlayer().Data.unlockedSmallBosses = t),
                new BoolPlayerDataSetting("Unlocked Big Monkeys", VanillaSprites.BigMonkeysModeIcon, false,
                    () => GetPlayer().Data.unlockedBigTowers, t => GetPlayer().Data.unlockedBigTowers = t),
                new BoolPlayerDataSetting("Unlocked Small Monkeys", VanillaSprites.SmallMonkeysModeIcon, false,
                    () => GetPlayer().Data.unlockedSmallTowers, t => GetPlayer().Data.unlockedSmallTowers = t),
                
                new NumberPlayerDataSetting("Tower Gift Unlock Pops", VanillaSprites.GiftBoxIcon, 0,
                    () => GetPlayer().Data.towerUnlockProgresses
                        .TryGetValue(GetPlayer().Data.selectedTowerForUnlockProgression ?? "editplayerdata", out var val)
                        ? val.ValueInt
                        : 0,
                    t =>
                    {
                        var dict = GetPlayer().Data.towerUnlockProgresses;
                        var key = GetPlayer().Data.selectedTowerForUnlockProgression ?? "editplayerdata";
                        if (dict.TryGetValue(key, out var val)) val.Value = t;
                        else dict[key] = new KonFuze_NoShuffle(t);
                    }),
                new NumberPlayerDataSetting("Odyssey Stars", VanillaSprites.OdysseyStarIcon, 0,
                    () => GetPlayer().Data.completedOdysseys.GetValues().ToList().Sum(v=>v.ValueInt+3),
                    t => GetPlayer().Data.completedOdysseys["EditPlayerData"] = new KonFuze_NoShuffle(
                        t - GetPlayer().Data.completedOdysseys.Keys().ToList().Where(k=>k != "EditPlayerData")
                            .Sum(k=>GetPlayer().Data.completedOdysseys[k].ValueInt+3) - 3)),
            }
        },
        {
            "Trophy Store", new List<PlayerDataSetting>() // uses a loop to reduce hard-coded values             
        },
        {
            "Skins", new List<PlayerDataSetting>() // uses a loop to reduce hard-coded values
        },
        {
            "Heroes", new List<PlayerDataSetting>() // uses a loop to reduce hard-coded values
        },
        {
            "Maps", new List<PlayerDataSetting>() // uses a loop to reduce hard-coded values
        },
        {
            "Maps - Coop", new List<PlayerDataSetting>() // uses a loop to reduce hard-coded values
        },
        {
            "Towers", new List<PlayerDataSetting>() // uses a loop to reduce hard-coded values
        },
        {
            "Powers", new List<PlayerDataSetting>() // uses a loop to reduce hard-coded values
        },
        {
            "Instas", new List<PlayerDataSetting>() // uses a loop to reduce hard-coded values
        },
        {
            "Banners", new List<PlayerDataSetting>() // uses a loop to reduce hard-coded values
        },
        {
            "Online Modes", new List<PlayerDataSetting>() // uses a loop to reduce hard-coded values
        },
        {
            "Achievements", new List<PlayerDataSetting>() // uses a loop to reduce hard-coded values
        },
        {
            "Stats", new List<PlayerDataSetting>
            {
                new KonFuzePlayerDataSetting("Games Played", VanillaSprites.HomePlayBtn, 0,
                    () => GetPlayer().Data.analyticsKonFuze.basicStats.gamesPlayed),
                new NumberPlayerDataSetting("Games Won", VanillaSprites.TrophyIcon, 0,
                    () => GetPlayer().Data.completedGame, t => GetPlayer().Data.completedGame = t),
                new NumberPlayerDataSetting("Highest Round (All Time)", VanillaSprites.BadIcon, 0,
                    () => GetPlayer().Data.highestSeenRound, t => GetPlayer().Data.highestSeenRound = t),
                new NumberPlayerDataSetting("Highest Round (Current Version)", VanillaSprites.ZomgIcon, 0,
                    () => GetPlayer().Data.highestSeenRoundCurrentVersion, t => GetPlayer().Data.highestSeenRoundCurrentVersion = t),
                new KonFuzePlayerDataSetting("Monkeys Placed", VanillaSprites.DartMonkey000, 0,
                    () => GetPlayer().Data.analyticsKonFuze.basicStats.totalTowersPlaced),
                new KonFuzePlayerDataSetting("Total Pop Count", VanillaSprites.Red, 0,
                    () => GetPlayer().Data.analyticsKonFuze.basicStats.bloonsPopped),
                new KonFuzePlayerDataSetting("Total Co-Op Pop Count", VanillaSprites.Coop2PlayerIcon, 0,
                    () => GetPlayer().Data.analyticsKonFuze.coopBloonsPopped),
                new KonFuzePlayerDataSetting("Camo Bloons Popped", VanillaSprites.CamoBloonIcon, 0,
                    () => GetPlayer().Data.analyticsKonFuze.basicStats.camosPopped),
                new KonFuzePlayerDataSetting("Lead Bloons Popped", VanillaSprites.LeadBloonIcon, 0,
                    () => GetPlayer().Data.analyticsKonFuze.basicStats.leadPopped),
                new KonFuzePlayerDataSetting("Purple Bloons Popped", VanillaSprites.PurpleBloonIcon, 0,
                    () => GetPlayer().Data.analyticsKonFuze.basicStats.purplesPopped),
                new KonFuzePlayerDataSetting("Regrow Bloons Popped", VanillaSprites.RegrowBloonIcon, 0,
                    () => GetPlayer().Data.analyticsKonFuze.basicStats.regrowPopped),
                new KonFuzePlayerDataSetting("Ceramic Bloons Popped", VanillaSprites.CeramicBloonIcon, 0,
                    () => GetPlayer().Data.analyticsKonFuze.basicStats.ceramicsPopped),
                new KonFuzePlayerDataSetting("Moabs Popped", VanillaSprites.MoabBloonIcon, 0,
                    () => GetPlayer().Data.analyticsKonFuze.basicStats.moabsPopped),
                new KonFuzePlayerDataSetting("Bfbs Popped", VanillaSprites.BfbIcon, 0,
                    () => GetPlayer().Data.analyticsKonFuze.basicStats.bfbsPopped),
                new KonFuzePlayerDataSetting("Zomgs Popped", VanillaSprites.ZomgIcon, 0,
                    () => GetPlayer().Data.analyticsKonFuze.basicStats.zomgsPopped),
                new KonFuzePlayerDataSetting("Ddts Popped", VanillaSprites.DdtIcon, 0,
                    () => GetPlayer().Data.analyticsKonFuze.basicStats.ddtsPopped),
                new KonFuzePlayerDataSetting("Bads Popped", VanillaSprites.BadIcon, 0,
                    () => GetPlayer().Data.analyticsKonFuze.basicStats.badsPopped),
                new KonFuzePlayerDataSetting("Bloons Leaked", VanillaSprites.LivesIcon, 0,
                    () => GetPlayer().Data.analyticsKonFuze.basicStats.bloonsLeaked),
                new KonFuzePlayerDataSetting("Cash Generated", VanillaSprites.StartingCash, 0,
                    () => GetPlayer().Data.analyticsKonFuze.basicStats.cashEarned),
                new KonFuzePlayerDataSetting("Cash Gifted", VanillaSprites.GiftBoxIcon, 0,
                    () => GetPlayer().Data.analyticsKonFuze.coopCashGiven),
                new KonFuzePlayerDataSetting("Abilities Used", VanillaSprites.ActivatedAbilityIcon, 0,
                    () => GetPlayer().Data.analyticsKonFuze.basicStats.totalAbilitiesActivated),
                new KonFuzePlayerDataSetting("Powers Used", VanillaSprites.PowersIcon, 0,
                    () => GetPlayer().Data.analyticsKonFuze.basicStats.totalPowersActivated),
                new KonFuzePlayerDataSetting("Insta Monkeys Used", VanillaSprites.InstasIcon, 0,
                    () => GetPlayer().Data.analyticsKonFuze.basicStats.instaMonkeysUsed),
                new NumberPlayerDataSetting("Daily Reward Chests Opened", VanillaSprites.DailyChestIcon, 0,
                    () => GetPlayer().Data.dailyRewardIndex, t => GetPlayer().Data.dailyRewardIndex = t),
                new KonFuzePlayerDataSetting("Daily Reward Chests Opened", VanillaSprites.DailyChestIcon, 0,
                    () => GetPlayer().Data.analyticsKonFuze.dailyReidCount),
                new NumberPlayerDataSetting("Challenges Completed", VanillaSprites.ChallengesIcon, 0,
                    () => GetPlayer().Data.totalDailyChallengesCompleted, t => GetPlayer().Data.totalDailyChallengesCompleted = t),
                new KonFuzePlayerDataSetting("Odysseys Completed", VanillaSprites.OdysseyStarIcon, 0,
                    () => GetPlayer().Data.totalCompletedOdysseys),
                new KonFuzePlayerDataSetting("Lifetime Trophies", VanillaSprites.TrophyIcon, 0,
                    () => GetPlayer().Data.lifetimeTrophies),
                new KonFuzePlayerDataSetting("Necro Bloons Reanimated", VanillaSprites.NecroBloonIcon, 0,
                    () => GetPlayer().Data.analyticsKonFuze.necroBloonsReanimated),
                new NumberPlayerDataSetting("Collection Chests Opened", VanillaSprites.CollectionEventLootIconChristmas, 0,
                    () => GetPlayer().Data.collectionEventCratesOpened, t => GetPlayer().Data.collectionEventCratesOpened = t),
                new NumberPlayerDataSetting("Golden Bloons Popped", VanillaSprites.GoldenBloonIcon, 0,
                    () => GetPlayer().Data.goldenBloonsPopped, t => GetPlayer().Data.goldenBloonsPopped = t),
                new NumberPlayerDataSetting("Monkey Team Wins", VanillaSprites.MonkeyTeamsIcon, 0,
                    () => GetPlayer().Data.monkeyTeamsWins, t => GetPlayer().Data.monkeyTeamsWins = t),
                new KonFuzePlayerDataSetting("Bosses Popped", VanillaSprites.BossT5Icon, 0,
                    () => GetPlayer().Data.analyticsKonFuze.basicStats.bossesPopped),
                new KonFuzePlayerDataSetting("Damage Done To Bosses", VanillaSprites.BossT1Icon, 0,
                    () => GetPlayer().Data.analyticsKonFuze.basicStats.damageDoneToBosses),
                
                new NumberPlayerDataSetting("Continues", VanillaSprites.ContinueIcon, 0,
                    () => GetPlayer().Data.continuesUsed.ValueInt, t => GetPlayer().Data.continuesUsed.Value = t),
                new NumberPlayerDataSetting("Challenges Played", VanillaSprites.ChallengesIcon, 0,
                    () => GetPlayer().Data.challengesPlayed.ValueInt, t => GetPlayer().Data.challengesPlayed.Value = t),
                new NumberPlayerDataSetting("Hosted Coop Games", VanillaSprites.CoOpIcon, 0,
                    () => GetPlayer().Data.hostedCoopGames, t => GetPlayer().Data.hostedCoopGames = t),
            }
        },
        {
            "Towers Placed", new List<PlayerDataSetting>() // uses a loop to reduce hard-coded values
        },
        {
            "Artifacts", new List<PlayerDataSetting>() // uses a loop to reduce hard-coded values 
        }
    };

    public static void SerializeAllSettings(FileStream file)
    {
        if (GetPlayer().OnlineData == null)
        {
            Settings["Online Modes"].RemoveAll(s => s.Name.StartsWith("CT")); // contested territory doesn't work w/o OnlineData
        }

        var writer = new Utf8JsonWriter(file);

        writer.WriteStartObject();
        foreach (var category in Settings.Keys)
        {
            writer.WriteStartObject(category);
            foreach (var setting in Settings[category])
            {
                writer.WritePropertyName(setting.GetId());
                setting.Serialize(writer);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndObject();

        writer.Dispose();
    }
    
    private static ReadOnlySpan<byte> Utf8Bom => new byte[] {0xEF, 0xBB, 0xBF};
    public static void DeserializeAllSettings(string file)
    {
        if (GetPlayer().OnlineData == null)
        {
            Settings["Online Modes"].RemoveAll(s => s.Name.StartsWith("CT")); // contested territory doesn't work w/o OnlineData
        }

        ReadOnlySpan<byte> jsonReadOnlySpan = File.ReadAllBytes(file);
        if (jsonReadOnlySpan.StartsWith(Utf8Bom)) jsonReadOnlySpan = jsonReadOnlySpan[Utf8Bom.Length..];

        var reader = new Utf8JsonReader(jsonReadOnlySpan);

        reader.Read(); // start object
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return;

            var category = reader.GetString()!;

            reader.Read(); // start object
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) break;

                var settingId = reader.GetString()!;

                reader.Read();
                Settings[category].Find(s => s.GetId() == settingId)?.Deserialize(ref reader);
            }
        }
    }

    private static bool _isOpen;

    private const int EntriesPerPage = 5;

    public static void InitSettings(ProfileModel data)
    {
        Settings["Trophy Store"].Clear();
        Settings["Skins"].Clear();
        Settings["Heroes"].Clear();
        Settings["Maps"].Clear();
        Settings["Maps - Coop"].Clear();
        Settings["Towers"].Clear();
        Settings["Powers"].Clear();
        Settings["Instas"].Clear();
        Settings["Banners"].Clear();
        Settings["Online Modes"].Clear();
        Settings["Towers Placed"].Clear();
        
        foreach (var item in GameData.Instance.trophyStoreItems.GetAllItems())
        {
            Settings["Trophy Store"].Add(new BoolPlayerDataSetting(item.GetLocalizedShortName()+" Enabled", item.icon.AssetGUID,
                false,
                () => Game.Player.EnabledTrophyStoreItems().Contains(item.Id),
                val => data.trophyStorePurchasedItems[item.Id].enabled = val
            ).Unlockable(
                () => !data.trophyStorePurchasedItems.ContainsKey(item.Id),
                () => Game.Player.AddTrophyStoreItem(item.id)));
        }
        
        foreach (var skin in GameData.Instance.skinsData.SkinList.items)
        {
            if (skin.isDefaultTowerSkin)
            {
                Settings["Heroes"].Add(new BoolPlayerDataSetting(
                    LocalizationManager.Instance.Format(skin.name), skin.icon.AssetGUID,
                    false, () => Game.Player.Data.unlockedHeroes.Contains(skin.name),
                    val =>
                    {
                        if (val) Game.Player.Data.unlockedHeroes.Add(skin.name);
                        else Game.Player.Data.unlockedHeroes.Remove(skin.name);
                    }));
                Settings["Towers Placed"].Add(new NumberPlayerDataSetting(
                    LocalizationManager.Instance.Format(skin.name),
                    skin.icon.AssetGUID, 0,
                    () =>
                    {
                        var dict = GetPlayer().Data.analyticsKonFuze.basicStats.heroesPlacedByName;
                        return dict.ContainsKey(skin.baseTowerName) ?
                            dict[skin.baseTowerName].ValueInt : 0;
                    },
                    t =>
                    {
                        var dict = GetPlayer().Data.analyticsKonFuze.basicStats.heroesPlacedByName;
                        if (t <= 0)
                        {
                            dict.Remove(skin.baseTowerName);
                            return;
                        }
                        
                        if (!dict.TryGetValue(skin.baseTowerName, out var ret))
                        {
                            ret = dict[skin.baseTowerName] = new KonFuze_NoShuffle();
                        }

                        ret.Value = t;
                    }));
                continue;
            }
            
            Settings["Skins"].Add(new BoolPlayerDataSetting(
                LocalizationManager.Instance.Format(skin.skinName),
                skin.icon.AssetGUID, false,
                () => GetPlayer().Data.unlockedTowerSkins.Contains(skin.name),
                val =>
                {
                    if (val) GetPlayer().Data.unlockedTowerSkins.Add(skin.name);
                    else GetPlayer().Data.unlockedTowerSkins.Remove(skin.name);
                }));
        }
        
        foreach (var achievement in GameData.Instance.achievements.achievements)
        {
            Settings["Achievements"].Add(new BoolPlayerDataSetting(
                LocalizationManager.Instance.Format(achievement.name),
                achievement.achievementIcon.AssetGUID, false,
                () => GetPlayer().Data.achievementsClaimed.Contains(achievement.achievementId),
                t =>
                {
                    Game.instance.achievementManager.TryGetActiveAchievement(achievement.achievementId, out var active);
                    
                    if (t)
                    {
                        Game.Player.Data.achievementsClaimed.Add(achievement.achievementId);
                        data.achievementsProgress[achievement.achievementId] = achievement.achievementGoal;
                        if (active == null) return;
                        active.claimed = true;
                        active.currentProgress = active.Goal;
                    }
                    else
                    {
                        Game.Player.Data.achievementsClaimed.Remove(achievement.achievementId);
                        data.achievementsProgress[achievement.achievementId] = 0;
                        if (active == null) return;
                        active.claimed = false;
                        active.currentProgress = 0;
                    }
                }));
        }

        foreach (var legend in Enum.GetValues<LegendType>())
        {
            foreach (var achievement in GameData.Instance.GetLegendsFeatsData(legend).featDatas)
            {
                Settings["Achievements"].Add(new BoolPlayerDataSetting(
                    LocalizationManager.Instance.Format(achievement.name),
                    achievement.icon.AssetGUID, false,
                    () => GetPlayer().Data.legendsData.featsClaimed.Contains(achievement.featId),
                    t =>
                    {
                        var id = achievement.featId;
                        var activeFeat = GameData.Instance.rogueData.featsData.GetActiveFeat(id);

                        if (t)
                        {
                            GetPlayer().Data.legendsData.featsClaimed.Add(id);
                            GetPlayer().Data.legendsData.featsProgress[id] = achievement.goal;
                            if (activeFeat == null) return;
                            activeFeat.claimed = true;
                            activeFeat.currentProgress = activeFeat.Goal;
                        }
                        else
                        {
                            GetPlayer().Data.legendsData.featsClaimed.Remove(id);
                            GetPlayer().Data.legendsData.featsProgress.Remove(id);
                            if (activeFeat == null) return;
                            activeFeat.claimed = false;
                            activeFeat.currentProgress = 0;
                        }
                    }));
            }
        }
        
        foreach (var details in GameData.Instance.mapSet.StandardMaps.ToIl2CppList())
        {
            Settings["Maps"].Add(new MapPlayerDataSetting(details, data.mapInfo.GetMap(details.id), false)
                .Unlockable(
                    () => !data.mapInfo.IsMapUnlocked(details.id),
                    () => data.mapInfo.UnlockMap(details.id)));
            Settings["Maps - Coop"].Add(new MapPlayerDataSetting(details, data.mapInfo.GetMap(details.id), true)
                .Unlockable(
                    () => !data.mapInfo.IsMapUnlocked(details.id),
                    () => data.mapInfo.UnlockMap(details.id)));
        }
        
        foreach (var power in Game.instance.model.powers)
        {
            if (power.name is "DungeonStatue" or "SpookyCreature") continue;

            Settings["Powers"].Add(new PowerPlayerDataSetting(power, GetPlayer).Unlockable(
                () => power.IsPowerPro && (!data.powersProSaveData.dataByPower.TryGetValue(power.PowerId, out var model) || model.unlockedTier.ValueInt == 0),
                () =>
                {
                    if (!data.powersProSaveData.dataByPower.ContainsKey(power.PowerId))
                    {
                        data.powersProSaveData.dataByPower[power.PowerId] = new PowersProPowerSaveData();
                    }
                    data.powersProSaveData.dataByPower[power.PowerId].unlockedTier.Value = 1;
                }));
        }

        foreach (var tower in Game.instance.GetTowerDetailModels())
        {
            if (tower.towerId == "Sheriff") continue;
            
            Settings["Towers"].Add(new TowerPlayerDataSetting(tower, GetPlayer).Unlockable(
                () => !data.unlockedTowers.Contains(tower.towerId),
                () =>
                {
                    Game.instance.towerGoalUnlockManager.CompleteGoalForTower(tower.towerId);
                    data.UnlockTower(tower.towerId);

                    foreach (var quest in Game.instance.questTrackerManager.QuestData.TowerUnlockQuestsContainer.items
                                 .ToList()
                                 .Where(quest => quest.towerId == tower.towerId))
                    {
                        var questData = Game.Player.GetQuestSaveData(quest.unlockQuestId);

                        questData.hasSeenQuest = true;
                        questData.hasSeenQuestCompleteDialogue = true;
                        questData.hasCollectedRewards = true;

                        foreach (var part in questData.questPartSaveData)
                        {
                            part.hasSeenQuestPart = true;
                            part.hasSeenQuestCompleteDialogue = true;
                            part.hasCollectedRewards = true;
                            part.completed = true;

                            foreach (var task in part.tasksSaveData)
                            {
                                task.hasCollectedRewards = true;
                                task.completed = true;
                            }
                        }

                        foreach (var task in questData.tasksSaveData)
                        {
                            task.hasCollectedRewards = true;
                            task.completed = true;
                        }

                        Game.Player.SetQuestSaveData(questData);
                    }
                }));

            Settings["Instas"].Add(new InstaMonkeyPlayerDataSetting(tower, GetPlayer));

            Settings["Towers Placed"].Add(new NumberPlayerDataSetting(
                LocalizationManager.Instance.Format(tower.towerId),
                tower.GetTower().icon.AssetGUID, 0,
                () =>
                {
                    var dict = GetPlayer().Data.analyticsKonFuze.basicStats.towersPlacedByBaseName;
                    return dict.ContainsKey(tower.towerId) ? dict[tower.towerId].ValueInt : 0;
                },
                t =>
                {
                    var dict = GetPlayer().Data.analyticsKonFuze.basicStats.towersPlacedByBaseName;
                    if (t <= 0)
                    {
                        dict.Remove(tower.towerId);
                        return;
                    }

                    if (!dict.TryGetValue(tower.towerId, out var ret))
                    {
                        ret = dict[tower.towerId] = new KonFuze_NoShuffle();
                    }

                    ret.Value = t;
                }));
        }
        
        foreach (var banner in GameData.Instance.profileBanners.profileBanners)
        {
            var storeItem = GameData.Instance.trophyStoreItems.GetStoreItem(banner.trophyStoreId);
            var name = storeItem?.GetLocalizedShortName() ?? "Default Banner";
            Settings["Banners"].Add(new ProfilePlayerDataSetting(name + (name.EndsWith(" Banner") ? "" : " Banner"),
                storeItem?.icon.AssetGUID ?? banner.iconSmall.AssetGUID, false,
                () => data.profileBanner == banner.id,
                t => data.profileBanner = t ? banner.id : GameData.Instance.profileBanners.defaultBanner.id));
        }
        foreach (var banner in GameData.Instance.teamsData.teamBanners.profileBanners)
        {
            var storeItem = GameData.Instance.teamsData.teamsStoreItems.GetStoreItem(banner.trophyStoreId);
            if (storeItem == null) continue; // no default banner for this one

            var name = storeItem.GetLocalizedShortName();
            Settings["Banners"].Add(new ProfilePlayerDataSetting(name + (name.EndsWith(" Banner") ? "" : " Banner"),
                storeItem.icon.AssetGUID, false,
                () => data.profileBanner == banner.id,
                t => data.profileBanner = t ? banner.id : GameData.Instance.profileBanners.defaultBanner.id));
        }
        
        foreach (var list in GameData.Instance.artifactsData.artifactModelsByType.Values())
        {
            foreach (var artifact in list) {
                Settings["Artifacts"].Add(new ArtifactPlayerDataSetting(artifact));
            }
        }

        foreach (var boss in Enum.GetValues<BossType>())
        {
            Settings["Online Modes"].Add(new NumberPlayerDataSetting($"{boss} Normal",
                VanillaSprites.ByName[$"{boss}Badge"], 0,
                () => GetPlayer().Data.bossMedals.ContainsKey((int)boss)
                    ? GetPlayer().Data.bossMedals[(int)boss].normalBadges.ValueInt
                    : 0,
                t =>
                {
                    if (!GetPlayer().Data.bossMedals.ContainsKey((int)boss))
                    {
                        GetPlayer().Data.bossMedals[(int)boss] = new BossMedalSaveData();
                    }

                    GetPlayer().Data.bossMedals[(int)boss].normalBadges.Value = t;
                }));
            Settings["Online Modes"].Add(new NumberPlayerDataSetting($"{boss} Elite",
                    VanillaSprites.ByName[$"{boss}EliteBadge"], 0,
                () => GetPlayer().Data.bossMedals.ContainsKey((int)boss)
                    ? GetPlayer().Data.bossMedals[(int)boss].eliteBadges.ValueInt
                    : 0,
                t =>
                {
                    if (!GetPlayer().Data.bossMedals.ContainsKey((int)boss))
                    {
                        GetPlayer().Data.bossMedals[(int)boss] = new BossMedalSaveData();
                    }

                    GetPlayer().Data.bossMedals[(int)boss].eliteBadges.Value = t;
                }));
        }

        var badgeToName = new Dictionary<LeaderboardBadgeType, string> {
            {LeaderboardBadgeType.BlackDiamond, "1st"},
            {LeaderboardBadgeType.RedDiamond, "2nd"},
            {LeaderboardBadgeType.BlueDiamond, "3rd"},
            {LeaderboardBadgeType.GoldDiamond, "Top 50"},
            {LeaderboardBadgeType.DoubleGold, "Top 1%"},
            {LeaderboardBadgeType.GoldSilver, "Top 10%"},
            {LeaderboardBadgeType.DoubleSilver, "Top 25%"},
            {LeaderboardBadgeType.Silver, "Top 50%"},
            {LeaderboardBadgeType.Bronze, "Top 75%"},
        };
        foreach (var leaderboard in badgeToName.Keys)
        {
            var name = leaderboard == LeaderboardBadgeType.BlueDiamond ? "Diamond" : leaderboard.ToString();
            Settings["Online Modes"].Add(new NumberPlayerDataSetting($"Boss {badgeToName[leaderboard]}",
                    VanillaSprites.ByName[$"BossMedalEvent{name}Medal"], 0,
                () => GetPlayer().Data.bossLeaderboardMedals.ContainsKey((int)leaderboard) ? GetPlayer().Data.bossLeaderboardMedals[(int)leaderboard].ValueInt : 0,
                t =>
                {
                    if (!GetPlayer().Data.bossLeaderboardMedals.ContainsKey((int)leaderboard))
                    {
                        GetPlayer().Data.bossLeaderboardMedals[(int)leaderboard] = new KonFuze_NoShuffle();
                    }
                    
                    GetPlayer().Data.bossLeaderboardMedals[(int)leaderboard].Value = t;
                }));
        }
        foreach (var leaderboard in badgeToName.Keys)
        {
            var name = leaderboard == LeaderboardBadgeType.BlueDiamond ? "Diamond" : leaderboard.ToString();
            Settings["Online Modes"].Add(new NumberPlayerDataSetting($"Elite Boss {badgeToName[leaderboard]}",
                VanillaSprites.ByName[$"EliteBossMedalEvent{name}Medal"], 0,
                () => GetPlayer().Data.bossLeaderboardEliteMedals.ContainsKey((int)leaderboard) ? GetPlayer().Data.bossLeaderboardEliteMedals[(int)leaderboard].ValueInt : 0,
                t =>
                {
                    if (!GetPlayer().Data.bossLeaderboardEliteMedals.ContainsKey((int)leaderboard))
                    {
                        GetPlayer().Data.bossLeaderboardEliteMedals[(int)leaderboard] = new KonFuze_NoShuffle();
                    }
                    
                    GetPlayer().Data.bossLeaderboardEliteMedals[(int)leaderboard].Value = t;
                }));
        }
        foreach (var leaderboard in badgeToName.Keys)
        {
            var name = leaderboard == LeaderboardBadgeType.BlueDiamond ? "Diamond" : leaderboard.ToString();
            Settings["Online Modes"].Add(new NumberPlayerDataSetting($"Race {badgeToName[leaderboard]}",
                    VanillaSprites.ByName[$"MedalEvent{name}Medal"], 0,
                () => GetPlayer().Data.raceMedalData.ContainsKey((int)leaderboard) ? GetPlayer().Data.raceMedalData[(int)leaderboard].ValueInt : 0,
                t =>
                {
                    if (!GetPlayer().Data.raceMedalData.ContainsKey((int)leaderboard))
                    {
                        GetPlayer().Data.raceMedalData[(int)leaderboard] = new KonFuze_NoShuffle();
                    }
                    
                    GetPlayer().Data.raceMedalData[(int)leaderboard].Value = t;
                }));
        }
        
        badgeToName = new Dictionary<LeaderboardBadgeType, string> {
            {LeaderboardBadgeType.BlackDiamond, "1st"},
            {LeaderboardBadgeType.RedDiamond, "2nd"},
            {LeaderboardBadgeType.BlueDiamond, "3rd"},
            {LeaderboardBadgeType.GoldDiamond, "4th-10th"},
            {LeaderboardBadgeType.DoubleGold, "11th-20th"},
            {LeaderboardBadgeType.Silver, "21st-40th"},
            {LeaderboardBadgeType.Bronze, "41st-60th"},
        };
        foreach (var leaderboard in badgeToName.Keys)
        {
            var name = leaderboard == LeaderboardBadgeType.BlueDiamond ? "Diamond" : leaderboard.ToString();
            Settings["Online Modes"].Add(new NumberPlayerDataSetting($"CT Local {badgeToName[leaderboard]}",
                    VanillaSprites.ByName[$"CtLocalPlayer{name}Medal"], 0,
                () => GetPlayer().GetCtLeaderboardBadges(false).ContainsKey((int)leaderboard) ? GetPlayer().GetCtLeaderboardBadges(false)[(int)leaderboard].ValueInt : 0,
                t =>
                {
                    if (!GetPlayer().GetCtLeaderboardBadges(false).ContainsKey((int)leaderboard))
                    {
                        GetPlayer().GetCtLeaderboardBadges(false)[(int)leaderboard] = new KonFuze_NoShuffle();
                    }

                    GetPlayer().GetCtLeaderboardBadges(false)[(int)leaderboard].Value = t;
                }));
        }
        
        badgeToName = new Dictionary<LeaderboardBadgeType, string> {
            {LeaderboardBadgeType.BlueDiamond, "Top 25"},
            {LeaderboardBadgeType.GoldDiamond, "Top 100"},
            {LeaderboardBadgeType.DoubleGold, "Top 1%"},
            {LeaderboardBadgeType.GoldSilver, "Top 10%"},
            {LeaderboardBadgeType.DoubleSilver, "Top 25%"},
            {LeaderboardBadgeType.Silver, "Top 50%"},
            {LeaderboardBadgeType.Bronze, "Top 75%"},
        };
        foreach (var leaderboard in badgeToName.Keys)
        {
            var name = leaderboard == LeaderboardBadgeType.BlueDiamond ? "Diamond" : leaderboard.ToString();
            Settings["Online Modes"].Add(new NumberPlayerDataSetting($"CT Global {badgeToName[leaderboard]}",
                VanillaSprites.ByName[$"CtGlobalPlayer{name}Medal"], 0,
                () => GetPlayer().GetCtLeaderboardBadges(true).ContainsKey((int)leaderboard) ? GetPlayer().GetCtLeaderboardBadges(true)[(int)leaderboard].ValueInt : 0,
                t =>
                {
                    if (!GetPlayer().GetCtLeaderboardBadges(true).ContainsKey((int)leaderboard))
                    {
                        GetPlayer().GetCtLeaderboardBadges(true)[(int)leaderboard] = new KonFuze_NoShuffle();
                    }

                    GetPlayer().GetCtLeaderboardBadges(true)[(int)leaderboard].Value = t;
                }));
        }
    }

    private int NumPages => (ActiveSettings.Count(s => s.Name.ContainsIgnoreCase(_searchValue))-1) / EntriesPerPage + 1;

    private List<PlayerDataSetting> ActiveSettings => _category == "All"
        ? Settings.Aggregate(new List<PlayerDataSetting>(), (l, kv) => l.Concat(kv.Value).ToList())
        : Settings[_category];
    
    private readonly PlayerDataSettingDisplay[] _entries = new PlayerDataSettingDisplay[EntriesPerPage];

    private static TMP_InputField? _searchInput;
    private string _searchValue = "";
    private string _category = "General";
    private int _pageIdx;

    private ModHelperPanel _topArea;

    private static Btd6Player GetPlayer()
    {
        return Game.Player;
    }

    public override bool OnMenuOpened(Object data)
    {
        _isOpen = true;

        if (GetPlayer().OnlineData == null)
        {
            Settings["Online Modes"].RemoveAll(s => s.Name.StartsWith("CT")); // contested territory doesn't work w/o OnlineData
        }
        
        GameMenu.GetComponentFromChildrenByName<NK_TextMeshProUGUI>("Title").SetText("Player Data");

        RemoveChild("TopBar");
        RemoveChild("Tabs");
        RemoveChild("RefreshBtn");
        GameMenu.requiresInternetObj.SetActive(false);

        GameMenu.previousPageBtn.SetOnClick(() => SetPage((_pageIdx - 1 + NumPages) % NumPages));
        GameMenu.nextPageBtn.SetOnClick(() => SetPage((_pageIdx + 1) % NumPages));
        GameMenu.previousPageBtn.interactable = GameMenu.nextPageBtn.interactable = true;
        
        var verticalLayoutGroup = GameMenu.scrollRect.content.GetComponent<VerticalLayoutGroup>();
        verticalLayoutGroup.SetPadding(50);
        verticalLayoutGroup.spacing = 50;
        verticalLayoutGroup.childControlWidth = true;
        verticalLayoutGroup.childControlHeight = true;
        GameMenu.scrollRect.rectTransform.sizeDelta += new Vector2(0, 200);
        GameMenu.scrollRect.rectTransform.localPosition += new Vector3(0, 100, 0);
        
        _topArea = GameMenu.GetComponentFromChildrenByName<RectTransform>("Container").gameObject
            .AddModHelperPanel(new Info("TopArea")
            {
                Y = -325, Height = 200, Pivot = new Vector2(0.5f, 1),
                AnchorMin = new Vector2(0, 1), AnchorMax = new Vector2(1, 1)
            }, layoutAxis: RectTransform.Axis.Horizontal, padding: 50);

        GenerateEntries();
        var options = new List<string> { "All" }.Concat(Settings.Keys).ToList();
        _topArea.AddDropdown(new Info("Category", 775, 150),
            options.ToIl2CppList(), 1850, new Action<int>(i =>
            {
                _category = options.ElementAt(i);
                SetPage(0);
            }), VanillaSprites.BlueInsertPanelRound, 80f).Dropdown.value = 1;
        _topArea.AddPanel(new Info("Spacing", InfoPreset.Flex));
        _searchInput = _topArea.AddInputField(new Info("Search", 1500, 150), _searchValue,
            VanillaSprites.BlueInsertPanelRound,
            new Action<string>(s =>
            {
                _searchValue = s;
                UpdateVisibleEntries();
            }),
            80f, TMP_InputField.CharacterValidation.None,
            TextAlignmentOptions.CaplineLeft, "Search...",
            50).InputField;
        
        _topArea.AddPanel(new Info("Spacing", InfoPreset.Flex));
        
        _topArea.AddButton(new Info("UnlockAll", 650, 200), VanillaSprites.GreenBtnLong, new Action(() =>
        {
            ActiveSettings.ForEach(s=>s.Unlock());
            UpdateVisibleEntries();
        })).AddText(new Info("UnlockAllText", 650, 200), "Unlock All", 60);
        _topArea.AddButton(new Info("SetAll", 650, 200), VanillaSprites.GreenBtnLong, new Action(() =>
        {
            PopupScreen.instance.SafelyQueue(screen =>
            {
                switch (_category)
                {
                    case "Powers":
                    {
                        NumberPlayerDataSetting.ShowPopup(screen, 0, n =>
                        {
                            foreach (var setting in ActiveSettings.Select(s => s as NumberPlayerDataSetting))
                            {
                                setting!.Setter(n);
                            }
                            UpdateVisibleEntries();
                        });
                        break;
                    }
                    case "Instas":
                    {
                        NumberPlayerDataSetting.ShowPopup(screen, 0, n =>
                        {
                            foreach (var setting in ActiveSettings.Select(s => s as InstaMonkeyPlayerDataSetting))
                            {
                                setting!.SetAll(n);
                            }
                            UpdateVisibleEntries();
                        });
                        break;
                    }
                    case "Artifacts" or "Achievements" or "Skins" or "Heroes":
                    {
                        BoolPlayerDataSetting.ShowPopup(screen, false, n =>
                        {
                            foreach (var setting in ActiveSettings.Select(s => s as BoolPlayerDataSetting))
                            {
                                setting!.Setter(n);
                            }
                            UpdateVisibleEntries();
                        });
                        break;
                    }
                }
            });
        })).AddText(new Info("SetAllText", 650, 200), "Set All", 60);
        _topArea.AddPanel(new Info("Special Button Filler", 650, 200));

        // for no discernible reason, this defaults to 300
        GameMenu.scrollRect.scrollSensitivity = 50;
        _searchInput.text = _searchValue = "";
        
        return false;
    }

    public override void OnMenuClosed()
    {
        _isOpen = false;
        
        Game.Player.SaveNow();
        _category = "General";
    }

    private void GenerateEntries()
    {
        GameMenu.scrollRect.content.GetComponentInChildren<HorizontalOrVerticalLayoutGroup>().spacing = 125;
        
        for (var i = 0; i < EntriesPerPage; i++)
        {
            _entries[i] = PlayerDataSettingDisplay.Generate($"Setting {i}");
            _entries[i].SetActive(false);
            _entries[i].AddTo(GameMenu.scrollRect.content);
        }
    }

    private void UpdateVisibleEntries()
    {
        var anyUnlockable = ActiveSettings.Any(s => !s.IsUnlocked());
        _topArea.GetDescendent<ModHelperButton>("UnlockAll")?.SetActive(anyUnlockable);

        var canAddAll = _category is "Powers" or "Instas" or "Artifacts" or "Skins" or "Heroes" or "Achievements";
        _topArea.GetDescendent<ModHelperButton>("SetAll")?.SetActive(!anyUnlockable && canAddAll);
        
        _topArea.GetDescendent<ModHelperPanel>("Special Button Filler")?.SetActive(!anyUnlockable && !canAddAll);

        var settings = ActiveSettings.FindAll(s => s.Name.ContainsIgnoreCase(_searchValue));
        SetPage(_pageIdx, false);
        
        for (var i = 0; i < EntriesPerPage; i++)
        {
            var idx = _pageIdx * EntriesPerPage + i;
            var entry = _entries[i];

            if (idx >= settings.Count)
            {
                entry.SetActive(false);
            }
            else
            {
                if (settings[idx].GetType() == typeof(MapPlayerDataSetting))
                {
                    ((MapPlayerDataSetting) settings[idx]).ReloadAllVisuals = UpdateVisibleEntries;
                }
                entry.SetSetting(settings[idx]);
                if (settings[idx].GetType() == typeof(ProfilePlayerDataSetting))
                {
                    settings[idx].ReloadVisuals = UpdateVisibleEntries; // needs to reload all visible
                }
                entry.SetActive(true);
            }
        }
    }

    private void SetPage(int page, bool updateEntries=true)
    {
        if (_pageIdx != page) GameMenu.scrollRect.verticalNormalizedPosition = 1f;
        _pageIdx = Mathf.Clamp(page, 0, NumPages-1);

        GameMenu.totalPages = NumPages;
        GameMenu.SetCurrentPage(_pageIdx + 1);

        // GameMenu.previousPageBtn.interactable = _pageIdx > 0;
        // GameMenu.nextPageBtn.interactable = _pageIdx < LastPage;

        if (updateEntries)
        {
            MenuManager.instance.buttonClick2Sound.Play("ClickSounds");
            UpdateVisibleEntries();            
        }
    }

    private void RemoveChild(string name)
    {
        GameMenu.GetComponentFromChildrenByName<RectTransform>(name).gameObject.active = false;
    }
    
    [HarmonyPatch(typeof(TMP_InputField), nameof(TMP_InputField.KeyPressed))]
    // ReSharper disable once InconsistentNaming
    internal class TMP_InputField_KeyPressed
    {
        [HarmonyPrefix]
        internal static void Prefix(TMP_InputField __instance, ref Event evt)
        {
            if (_isOpen && __instance != _searchInput && (evt.character == '-' || !int.TryParse(__instance.text + evt.character, out _)))
            {
                evt.character = (char) 0;                
            }
        }
    }
}