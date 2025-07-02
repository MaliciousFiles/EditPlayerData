using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Data.Artifacts;
using Il2CppAssets.Scripts.Data.Boss;
using Il2CppAssets.Scripts.Data.TrophyStore;
using Il2CppAssets.Scripts.Models.Artifacts;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Models.Store.Loot;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Menu;
using Il2CppAssets.Scripts.Unity.Player;
using Il2CppAssets.Scripts.Unity.UI_New.Achievements;
using Il2CppAssets.Scripts.Unity.UI_New.ChallengeEditor;
using Il2CppAssets.Scripts.Unity.UI_New.Legends;
using Il2CppAssets.Scripts.Unity.UI_New.Odyssey;
using Il2CppAssets.Scripts.Unity.UI_New.Popups;
using Il2CppAssets.Scripts.Utils;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using Il2CppNinjaKiwi.Common;
using Il2CppNinjaKiwi.LiNK.Client.Streams;
using Il2CppSystem.Linq;
using Il2CppSystem.Text;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;
using Action = System.Action;
using Enum = System.Enum;
using Object = Il2CppSystem.Object;

namespace EditPlayerData.UI;

public class EditPlayerDataMenu : ModGameMenu<ContentBrowser>
{
    private static readonly Dictionary<string, List<PlayerDataSetting>> Settings = new()
    {
        {
            "General", new List<PlayerDataSetting>
            {
                new PurchasePlayerDataSetting("Unlocked Double Cash", VanillaSprites.DoubleCashModeShop, "btd6_doublecashmode"),
                new PurchasePlayerDataSetting("Unlocked Fast Track", VanillaSprites.FastTrackModeIcon,
                    "btd6_fasttrackpack",
                    () => GetPlayer().Data.unlockedFastTrack,
                    t => GetPlayer().Data.unlockedFastTrack = t),
                new PurchasePlayerDataSetting("Unlocked Rogue Legends", VanillaSprites.LegendsBtn, "btd6_legendsrogue"),
                new PurchasePlayerDataSetting("Unlocked Map Editor", VanillaSprites.MapEditorBtn, "btd6_mapeditorsupporter_new"),
                new NumberPlayerDataSetting("Monkey Money", VanillaSprites.MonkeyMoneyShop, 0,
                    () => GetPlayer().Data.monkeyMoney.ValueInt, t => GetPlayer().Data.monkeyMoney.Value = t),
                new NumberPlayerDataSetting("Monkey Knowledge", VanillaSprites.KnowledgeIcon, 0,
                    () => GetPlayer().Data.knowledgePoints.ValueInt, t => GetPlayer().Data.knowledgePoints.Value = t),
                new RankPlayerDataSetting(GetPlayer),
                
                new NumberPlayerDataSetting("Trophies", VanillaSprites.TrophyIcon, 0,
                    () => GetPlayer().Data.trophies.ValueInt,
                    t => GetPlayer().GainTrophies(t - GetPlayer().Data.trophies.ValueInt, "")),
                new NumberPlayerDataSetting("Games Won", VanillaSprites.ConfettiIcon, 0,
                    () => GetPlayer().Data.completedGame, t => GetPlayer().Data.completedGame = t),
                new NumberPlayerDataSetting("Rogue XP", VanillaSprites.RogueXpShopIconLarge, 0,
                    () => GetPlayer().Data.legendsData.rogueLegendXp, t => GetPlayer().Data.legendsData.rogueLegendXp = t),
                new NumberPlayerDataSetting("Odyssey Stars", VanillaSprites.OdysseyStarIcon, 0,
                    () => GetPlayer().Data.completedOdysseys.GetValues().ToList().Sum(v=>v.ValueInt+3),
                    t => GetPlayer().Data.completedOdysseys["EditPlayerData"] = new KonFuze_NoShuffle(
                        t - GetPlayer().Data.completedOdysseys.Keys().ToList().Where(k=>k != "EditPlayerData")
                            .Sum(k=>GetPlayer().Data.completedOdysseys[k].ValueInt+3) - 3)),
                new NumberPlayerDataSetting("Highest Seen Round", VanillaSprites.BadBloonIcon, 0,
                    () => GetPlayer().Data.highestSeenRound, t => GetPlayer().Data.highestSeenRound = t),
                new NumberPlayerDataSetting("Continues", VanillaSprites.ContinueIcon, 0,
                    () => GetPlayer().Data.continuesUsed.ValueInt, t => GetPlayer().Data.continuesUsed.Value = t),
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
                
                new NumberPlayerDataSetting("Challenges Shared", VanillaSprites.CreateChallengesIcon, 0,
                    () => GetPlayer().Data.challengesShared.ValueInt, t => GetPlayer().Data.challengesShared.Value = t),
                new NumberPlayerDataSetting("Challenges Played", VanillaSprites.ChallengesIcon, 0,
                    () => GetPlayer().Data.challengesPlayed.ValueInt, t => GetPlayer().Data.challengesPlayed.Value = t),
                new NumberPlayerDataSetting("Odysseys Completed", VanillaSprites.OdysseyIcon, 0,
                    () => GetPlayer().Data.totalCompletedOdysseys.ValueInt,
                    t => GetPlayer().Data.totalCompletedOdysseys.Value = t),
                new NumberPlayerDataSetting("Rogue Feats", VanillaSprites.Legends1, 0,
                    () =>
                    {
                        var sum = 0;
                        foreach (var f in GameData.Instance.rogueData.featsData.featDatas)
                        {
                            if (GetPlayer().Data.legendsData.featsClaimed.Contains(f.featId)) sum++;
                        }
                        return sum;
                    },
                    t =>
                    {
                        var feats = GameData.Instance.rogueData.featsData.featDatas;
                        for (var i = 0; i < feats.Count; i++)
                        {
                            var id = feats.Get(i).featId;
                            var activeFeat = GameData.Instance.rogueData.featsData.GetActiveFeat(id);

                            if (i < t)
                            {
                                GetPlayer().Data.legendsData.featsClaimed.Add(id);
                                activeFeat.claimed = true;
                                GetPlayer().Data.legendsData.featsProgress[id] = feats.Get(i).goal;
                                activeFeat.currentProgress = activeFeat.Goal;
                            }
                            else
                            {
                                GetPlayer().Data.legendsData.featsClaimed.Remove(id);
                                activeFeat.claimed = false;
                                GetPlayer().Data.legendsData.featsProgress.Remove(id);
                                activeFeat.currentProgress = 0;
                            }
                        }
                        
                        GetPlayer().SetLegendBadges(nameof(LegendsType.Rogue), false, Mathf.Min(t, feats.Count));
                        GetPlayer().SetLegendBadges(nameof(LegendsType.Rogue), true, t >= feats.Count ? 1 : 0);
                    }),
                new NumberPlayerDataSetting("Tower Gift Unlock Pops", VanillaSprites.GiftBoxIcon, 0,
                    () => GetPlayer().Data.towerUnlockProgresses
                        .TryGetValue(GetPlayer().Data.selectedTowerForUnlockProgression, out var val)
                        ? val.ValueInt
                        : 0,
                    t =>
                    {
                        var dict = GetPlayer().Data.towerUnlockProgresses;
                        var key = GetPlayer().Data.selectedTowerForUnlockProgression;
                        if (dict.TryGetValue(key, out var val)) val.Value = t;
                        else dict[key] = new KonFuze_NoShuffle(t);
                    }),
                new NumberPlayerDataSetting("Daily Reward Index", VanillaSprites.DailyChestIcon, 0,
                    () => GetPlayer().Data.dailyRewardIndex, t => GetPlayer().Data.dailyRewardIndex = t),
                
                new NumberPlayerDataSetting("Total Daily Challenges Completed", VanillaSprites.ChallengeTrophyIcon, 0,
                    () => GetPlayer().Data.totalDailyChallengesCompleted,
                    t => GetPlayer().Data.totalDailyChallengesCompleted = t),
                new NumberPlayerDataSetting("Consecutive Daily Challenges Completed",
                    VanillaSprites.ChallengeThumbsUpIcon, 0,
                    () => GetPlayer().Data.consecutiveDailyChallengesCompleted,
                    t => GetPlayer().Data.consecutiveDailyChallengesCompleted = t),
                new NumberPlayerDataSetting("Challenges Played", VanillaSprites.ChallengesIcon, 0,
                    () => GetPlayer().Data.challengesPlayed.ValueInt, t => GetPlayer().Data.challengesPlayed.Value = t),
                new NumberPlayerDataSetting("Hosted Coop Games", VanillaSprites.CoOpIcon, 0,
                    () => GetPlayer().Data.hostedCoopGames, t => GetPlayer().Data.hostedCoopGames = t),
                new NumberPlayerDataSetting("Collection Event Crates Opened",
                    VanillaSprites.CollectionEventLootIconEaster, 0,
                    () => GetPlayer().Data.collectionEventCratesOpened,
                    t => GetPlayer().Data.collectionEventCratesOpened = t),
                
                new NumberPlayerDataSetting("Golden Bloons Popped", VanillaSprites.GoldenBloonIcon, 0,
                    () => GetPlayer().Data.goldenBloonsPopped, t => GetPlayer().Data.goldenBloonsPopped = t),
            }
        },
        {
            "Trophy Store", new List<PlayerDataSetting>() // uses a loop to reduce hard-coded values             
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
            "Artifacts", new List<PlayerDataSetting>() // uses a loop to reduce hard-coded values 
        },
        {
            "Online Modes", new List<PlayerDataSetting>() // uses a loop to reduce hard-coded values
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
    
    private static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];
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
        Settings["Maps"].Clear();
        Settings["Maps - Coop"].Clear();
        Settings["Towers"].Clear();
        Settings["Powers"].Clear();
        Settings["Instas"].Clear();
        Settings["Banners"].Clear();
        Settings["Online Modes"].Clear();
        
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
            if (power.name is "CaveMonkey" or "DungeonStatue" or "SpookyCreature") continue;

            Settings["Powers"].Add(new NumberPlayerDataSetting(
                LocalizationManager.Instance.Format(power.name),
                power.icon.GetGUID(), 0,
                () => GetPlayer().GetPowerData(power.name)?.Quantity ?? 0,
                t =>
                {
                    if (GetPlayer().IsPowerAvailable(power.name))
                    {
                        GetPlayer().GetPowerData(power.name).Quantity = t;
                    }
                    else
                    {
                        GetPlayer().AddPower(power.name, t);
                    }
                }));
        }

        foreach (var tower in Game.instance.GetTowerDetailModels())
        {
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
        
        foreach (var artifact in GameData.Instance.artifactsData.artifactModelsByType[Il2CppType.Of<ItemArtifactModel>()])
        {
            Settings["Artifacts"].Add(new ArtifactPlayerDataSetting(artifact));
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

    private int LastPage => (Settings[_category].Count(s => s.Name.ContainsIgnoreCase(_searchValue))-1) / EntriesPerPage;

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

        GameMenu.firstPageBtn.SetOnClick(() => SetPage(0));
        GameMenu.previousPageBtn.SetOnClick(() => SetPage(_pageIdx - 1));
        GameMenu.nextPageBtn.SetOnClick(() => SetPage(_pageIdx + 1));
        GameMenu.lastPageBtn.SetOnClick(() => SetPage(LastPage));

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

        _topArea.AddDropdown(new Info("Category", 775, 150),
            Settings.Keys.ToIl2CppList(), 2000, new Action<int>(i =>
            {
                _category = Settings.Keys.ElementAt(i);
                SetPage(0);
            }), VanillaSprites.BlueInsertPanelRound, 80f);
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
            Settings[_category].ForEach(s=>s.Unlock());
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
                            foreach (var setting in Settings[_category].Select(s => s as NumberPlayerDataSetting))
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
                            foreach (var setting in Settings[_category].Select(s => s as InstaMonkeyPlayerDataSetting))
                            {
                                setting!.SetAll(n);
                            }
                            UpdateVisibleEntries();
                        });
                        break;
                    }
                    case "Artifacts":
                    {
                        BoolPlayerDataSetting.ShowPopup(screen, false, n =>
                        {
                            foreach (var setting in Settings[_category].Select(s => s as ArtifactPlayerDataSetting))
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

        
        GenerateEntries();
        SetPage(0);

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
        var anyUnlockable = Settings[_category].Any(s => !s.IsUnlocked());
        _topArea.GetDescendent<ModHelperButton>("UnlockAll")?.SetActive(anyUnlockable);

        var canAddAll = _category is "Powers" or "Instas" or "Artifacts";
        _topArea.GetDescendent<ModHelperButton>("SetAll")?.SetActive(!anyUnlockable && canAddAll);
        
        _topArea.GetDescendent<ModHelperPanel>("Special Button Filler")?.SetActive(!anyUnlockable && !canAddAll);

        var settings = Settings[_category].FindAll(s => s.Name.ContainsIgnoreCase(_searchValue));
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
        _pageIdx = Mathf.Clamp(page, 0, LastPage);

        GameMenu.totalPages = LastPage + 1;
        GameMenu.SetCurrentPage(_pageIdx + 1);

        GameMenu.firstPageBtn.interactable = GameMenu.previousPageBtn.interactable = _pageIdx > 0;
        GameMenu.lastPageBtn.interactable = GameMenu.nextPageBtn.interactable = _pageIdx < LastPage;

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