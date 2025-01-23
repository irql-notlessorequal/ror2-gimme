using BepInEx;
using R2API;


using RoR2;

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.Networking;
using BepInEx.Logging;
using System.IO;

namespace Gimme
{
    // This is an example plugin that can be put in
    // BepInEx/plugins/ExamplePlugin/ExamplePlugin.dll to test out.
    // It's a small plugin that adds a relatively simple item to the game,
    // and gives you that item whenever you press F2.

    // This attribute specifies that we have a dependency on a given BepInEx Plugin,
    // We need the R2API ItemAPI dependency because we are using for adding our item to the game.
    // You don't need this if you're not using R2API in your plugin,
    // it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(ItemAPI.PluginGUID)]

    // This one is because we use a .language file for language tokens
    // More info in https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
    [BepInDependency(LanguageAPI.PluginGUID)]

    // This attribute is required, and lists metadata for your plugin.
    [BepInPlugin("com.nulldev.ror2.gimme", "Gimme", "0.0.4")]

    // This is the main declaration of our plugin class.
    // BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    // BaseUnityPlugin itself inherits from MonoBehaviour,
    // so you can use this as a reference for what you can declare and use in your plugin class
    // More information in the Unity Docs: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class Main : BaseUnityPlugin
    {
        internal static new ManualLogSource log { get; set; }

        private void Awake()
        {
            log = base.Logger;

            // ISSUE: method pointer
            On.RoR2.Console.RunCmd += Console_RunCmd;
            On.RoR2.UserAchievementManager.GrantAchievement += UserAchievementManager_GrantAchievement;
            On.RoR2.AchievementGranter.CallRpcGrantAchievement += AchievementGranter_CallRpcGrantAchievement;
            On.RoR2.AchievementGranter.RpcGrantAchievement += AchievementGranter_RpcGrantAchievement;
            On.RoR2.AchievementGranter.InvokeRpcRpcGrantAchievement += AchievementGranter_InvokeRpcRpcGrantAchievement;

            log.LogInfo("Gimme loaded successfully.");
        }

        private static void AchievementGranter_CallRpcGrantAchievement(On.RoR2.AchievementGranter.orig_CallRpcGrantAchievement orig, AchievementGranter self, string achievementName)
        {
            /* Prevent achievements since this plugin makes it really easy to get them. */
            log.LogDebug("[AchievementGranter::CallRpcGrantAchievement] Discarding the following achievement: " + achievementName);
        }

        private static void AchievementGranter_RpcGrantAchievement(On.RoR2.AchievementGranter.orig_RpcGrantAchievement orig, AchievementGranter self, string achievementName)
        {
            log.LogDebug("[AchievementGranter::RpcGrantAchievement] Discarding the following achievement: " + achievementName);
        }

        private static void AchievementGranter_InvokeRpcRpcGrantAchievement(On.RoR2.AchievementGranter.orig_InvokeRpcRpcGrantAchievement orig, NetworkBehaviour obj, NetworkReader reader)
        {
            log.LogDebug("[AchievementGranter::InvokeRpcRpcGrantAchievement] Call discarded. ");
        }

        private static void UserAchievementManager_GrantAchievement(On.RoR2.UserAchievementManager.orig_GrantAchievement orig, UserAchievementManager self, AchievementDef achievementDef)
        {
            log.LogDebug("[UserAchievementManager::GrantAchievement] Discarding the following achievement: " + achievementDef);
        }

        private static void Console_RunCmd(On.RoR2.Console.orig_RunCmd orig, RoR2.Console self, RoR2.Console.CmdSender sender, string concommandName, List<string> userArgs)
        {
            if (!NetworkServer.active || Run.instance == null || !concommandName.Equals("say", System.StringComparison.InvariantCultureIgnoreCase))
            {
                orig.Invoke(self, sender, concommandName, userArgs);
            }
            else
            {
                string str1 = userArgs.FirstOrDefault<string>();
                if (string.IsNullOrWhiteSpace(str1) || !str1.StartsWith("/"))
                {
                    orig.Invoke(self, sender, concommandName, userArgs);
                }
                else
                {
                    string[] source = str1.Split(' ');
                    string str2 = ((IEnumerable<string>)source).FirstOrDefault<string>().Substring(1);
                    string[] array = ((IEnumerable<string>)source).Skip<string>(1).ToArray<string>();
                    if (str2.ToUpperInvariant() == "GIMME" || str2.ToUpperInvariant() == "GI")
                    {
                        Chat.SendBroadcastChat((ChatMessageBase)new Chat.UserChatMessage()
                        {
                            sender = ((Component)sender.networkUser).gameObject,
                            text = str1
                        });
                        if (array.Length < 2 || array[0] == "" || array[0].ToUpperInvariant() == "HELP")
                        {
                            Chat.SendBroadcastChat((ChatMessageBase)new Chat.SimpleChatMessage()
                            {
                                baseToken = "<color=#AAE6F0>/gi itemname playername [amount]\n/gimme itemname playername [amount]\n/gr [itemname] [amount]\n/gimmerandom [itemname] [amount]\nWill give items into playername's inventory"
                            });
                        }
                        else
                        {
                            string str5 = Give.Give_item(sender.networkUser, array, log);
                            if (str5 == null)
                                Chat.SendBroadcastChat((ChatMessageBase)new Chat.SimpleChatMessage()
                                {
                                    baseToken = "<color=#ff4646>ERROR: null output</color>"
                                });
                            else
                                Chat.SendBroadcastChat((ChatMessageBase)new Chat.SimpleChatMessage()
                                {
                                    baseToken = str5
                                });
                        }
                    }
                    else if (str2.ToUpperInvariant() == "GR" || str2.ToUpperInvariant() == "GIMMERANDOM")
                    {
                        Chat.SendBroadcastChat((ChatMessageBase)new Chat.UserChatMessage()
                        {
                            sender = ((Component)sender.networkUser).gameObject,
                            text = str1
                        });

                        string str5 = Give.Give_item_random(sender.networkUser, array, log);
                        if (str5 == null)
                            Chat.SendBroadcastChat((ChatMessageBase)new Chat.SimpleChatMessage()
                            {
                                baseToken = "<color=#ff4646>ERROR: null output</color>"
                            });
                        else
                            Chat.SendBroadcastChat((ChatMessageBase)new Chat.SimpleChatMessage()
                            {
                                baseToken = str5
                            });
                    }
                    else if (str2.ToUpperInvariant() == "GIMME_DUMP_ITEMS")
                    {
                        Give.Dump_items();
                        Chat.SendBroadcastChat((ChatMessageBase)new Chat.SimpleChatMessage()
                        {
                            baseToken = "Gimme wrote a gimme_items.txt into your game's directory."
                        });
                    }
                    else
                    {
                        orig.Invoke(self, sender, concommandName, userArgs);
                    }
                }
            }
        }
    }
    internal class Give
    {
        public const string green = "<color=#96EBAA>";
        public const string player = "<color=#AAE6F0>";
        public const string error = "<color=#FF8282>";
        public const string bold = "<color=#ff4646>";

        private static readonly Dictionary<ItemDef, int> RESTRICTED_ITEMS = new Dictionary<ItemDef, int>();

        static Give()
        {
            /* Too many Shaped Glass will put characters into a respawn loop, which eventually will explode their session */
            RESTRICTED_ITEMS.Add(RoR2Content.Items.LunarDagger, 64);
            /* 1024 Bottled Chaos makes my game lag (on 2017-era hardware) for a while */
            RESTRICTED_ITEMS.Add(DLC1Content.Items.RandomEquipmentTrigger, 128);
            /* Limit movement speed boosts to one hundred, otherwise you will literally hit world bounds instantly */
            RESTRICTED_ITEMS.Add(DLC1Content.Items.AttackSpeedAndMoveSpeed, 100);
            RESTRICTED_ITEMS.Add(RoR2Content.Items.SprintBonus, 100);
            RESTRICTED_ITEMS.Add(RoR2Content.Items.Hoof, 100);
            /* Limit the amount of Wax Quail's */
            RESTRICTED_ITEMS.Add(RoR2Content.Items.JumpBoost, 10);
            /* Limit jump heights with H3AD-5T V2 */
            RESTRICTED_ITEMS.Add(RoR2Content.Items.FallBoots, 10);
            /* Prevent instantaenous equipment spam */
            RESTRICTED_ITEMS.Add(RoR2Content.Items.AutoCastEquipment, 32);
            RESTRICTED_ITEMS.Add(DLC1Content.Items.HalfAttackSpeedHalfCooldowns, 8);
            RESTRICTED_ITEMS.Add(RoR2Content.Items.Talisman, 69);
            /* Prevent literally being unable to move */
            RESTRICTED_ITEMS.Add(DLC1Content.Items.HalfSpeedDoubleHealth, 16);
        }

        public static string Give_item_random(NetworkUser sender, string[] args, ManualLogSource log)
        {
            NetworkUserId id = sender.id;
            Inventory inventory1 = sender != null ? sender.master.inventory : (Inventory)null;

            NetworkUser netUserFromString = StringParsers.GetRandomUser();

            Inventory inventory2 = netUserFromString != null ? netUserFromString.master.inventory : (Inventory)null;
            if (!inventory1 || !inventory2)
                return "<color=#ff4646>ERROR: null inventory</color>";

            int num = 1;

            if (args.Length == 2)
            {
                if (!System.Int32.TryParse(args[1], out num))
                {
                    return "<color=#FF8282>Invalid quantity argument!</color>";
                }
            }

            return _provide_item(sender, inventory1, inventory2, netUserFromString, num, args, log);
        }

        private static string last(string[] arr)
        {
            return arr[arr.Length - 1];
        }

        public static string Give_item(NetworkUser sender, string[] args, ManualLogSource log)
        {
            NetworkUserId id = sender.id;

            Inventory inventory1 = sender != null ? sender.master.inventory : (Inventory)null;
            NetworkUser netUserFromString = StringParsers.GetNetUserFromString(args[1]);
            if (netUserFromString == null)
                return "<color=#FF8282>Could not find specified </color>player<color=#FF8282> '<color=#ff4646>" + args[1] + "</color>'</color>";

            Inventory inventory2 = netUserFromString != null ? netUserFromString.master.inventory : (Inventory)null;
            if (!inventory1 || !inventory2)
                return "<color=#ff4646>ERROR: null inventory</color>";

            int num = 1;

            if (args.Length == 3)
            {
                if (!System.Int32.TryParse(args[2], out num))
                {
                    return "<color=#FF8282>Invalid quantity argument!</color>";
                }
            }

            return _provide_item(sender, inventory1, inventory2, netUserFromString, num, args, log);
        }

        public static string _provide_item(NetworkUser sender, Inventory inventory1, Inventory inventory2, NetworkUser netUserFromString, int num, string[] args, ManualLogSource log)
        {
            string str1 = "<color=#AAE6F0>" + netUserFromString.masterController.GetDisplayName() + "</color>";
            string str2 = "<color=#AAE6F0>" + sender.masterController.GetDisplayName() + "</color>";
            EquipmentIndex equipmentIndex = EquipmentIndex.None;

            ItemIndex itemIndex = ItemIndex.None;

            if (args.Length == 0)
            {
                itemIndex = StringParsers.RandomItem();
            }
            else
            {
                if (args[0].ToLower() == "e" || args[0].ToLower() == "equip" || args[0].ToLower() == "equipment")
                {
                    equipmentIndex = inventory1.GetEquipmentIndex();
                    if (equipmentIndex == EquipmentIndex.None)
                        return "<color=#FF8282>Sender does not have an </color><color=#ff7d00>equipment</color>";
                }

                if (itemIndex == ItemIndex.None)
                    itemIndex = StringParsers.FindItem(args[0], log);
            }

            if (itemIndex == ItemIndex.None)
                return "<color=#FF8282>Could not find specified </color>item<color=#FF8282> '<color=#ff4646>" + args[0] + "</color>'</color>";

            ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
            PickupIndex pickupIndex1 = PickupCatalog.FindPickupIndex(itemIndex);
            string coloredString1 = Util.GenerateColoredString(Language.GetString(itemDef.nameToken), PickupCatalog.GetPickupDef(pickupIndex1).baseColor);

            if (itemDef == RoR2Content.Items.CaptainDefenseMatrix || itemDef.tier == ItemTier.NoTier && itemDef != RoR2Content.Items.ExtraLifeConsumed && itemDef != DLC1Content.Items.ExtraLifeVoidConsumed && itemDef != DLC1Content.Items.FragileDamageBonusConsumed && itemDef != DLC1Content.Items.HealingPotionConsumed && itemDef != DLC1Content.Items.RegeneratingScrapConsumed)
                return coloredString1 + "<color=#FF8282> is not dropable</color>";

            if (RESTRICTED_ITEMS.ContainsKey(itemDef))
            {
                /* Check if we are about to give too much to a player and if so, don't do it */
                int currentAmount = inventory2.GetItemCount(itemDef);
                int limit = RESTRICTED_ITEMS[itemDef];

                if (num >= limit)
                {
                    return "<color=#FF8282>Too much of item requested, the limit is '" + (limit - 1) + "'.</color>";
                }

                if (currentAmount + num >= limit)
                {
                    return "<color=#FF8282>Player already has too much of item.</color>";
                }
            }

            if (num > 1)
                coloredString1 += Util.GenerateColoredString("s", PickupCatalog.GetPickupDef(pickupIndex1).baseColor);

            if (num > 1024)
                num = 1024;
            inventory2.GiveItem(itemIndex, num);

            if (inventory1 != inventory2)
                inventory1.RemoveItem(itemIndex, num);

            return string.Format("{0}{1} gave {2} {3} to </color>{4}", (object)"<color=#96EBAA>", (object)str2, (object)num, (object)coloredString1, (object)str1);
        }

        internal static void Dump_items()
        {
            using (StreamWriter text = new StreamWriter("gimme_items.txt"))
            {
                text.WriteLine("// Gimme FORMAT_ONE");
                foreach (ItemIndex item in ItemCatalog.allItems)
                {
                    ItemDef itemDefinition = ItemCatalog.GetItemDef(item);
                    var localizedName = Language.GetString(itemDefinition.nameToken);

                    /* So fugly... */
                    string str = "";

                    str += "{index:";
                    str += item;

                    str += ",tier:";
                    str += ((int)itemDefinition.tier);

                    str += ",nameToken:\"";
                    str += itemDefinition.name;

                    str += "\",localizedName:\"";
                    str += localizedName;
                    str += "\"}";

                    text.WriteLine(str);
                }
            }
        }
    }

    internal sealed class StringParsers
    {
        internal static ItemIndex FindItemInInventory(string input, Inventory inventory)
        {
            List<ItemIndex> itemAcquisitionOrder = inventory.itemAcquisitionOrder;
            if (!itemAcquisitionOrder.Any())
            {
                return (ItemIndex)(-1);
            }
            input = ReformatString(input);
            if (int.TryParse(input, out var result))
            {
                if (result > itemAcquisitionOrder.Count || result < 0)
                {
                    return (ItemIndex)(-1);
                }
                if (result == 0)
                {
                    return itemAcquisitionOrder[itemAcquisitionOrder.Count - 1];
                }
                return itemAcquisitionOrder[itemAcquisitionOrder.Count - result];
            }
            for (int num = itemAcquisitionOrder.Count - 1; num >= 0; num--)
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(itemAcquisitionOrder[num]);
                if (ReformatString(Language.GetString(itemDef.nameToken)).Contains(input))
                {
                    return itemDef.itemIndex;
                }
            }
            return (ItemIndex)(-1);
        }

        public static EquipmentIndex GetEquipFromPartial(string name)
        {
            //IL_002f: Unknown result type (might be due to invalid IL or missing references)
            name = ReformatString(name);
            EquipmentDef[] equipmentDefs = EquipmentCatalog.equipmentDefs;
            foreach (EquipmentDef val in equipmentDefs)
            {
                if (ReformatString(Language.GetString(val.nameToken)).Contains(name))
                {
                    return val.equipmentIndex;
                }
            }
            return (EquipmentIndex)(-1);
        }

        private static readonly System.Random rng = new System.Random();

        internal static NetworkUser GetRandomUser()
        {
            return NetworkUser.readOnlyInstancesList[rng.Next(NetworkUser.readOnlyInstancesList.Count)];
        }

        internal static NetworkUser GetNetUserFromString(string name)
        {
            if (int.TryParse(name, out var result))
            {
                if (result < NetworkUser.readOnlyInstancesList.Count && result >= 0)
                {
                    return NetworkUser.readOnlyInstancesList[result];
                }
                return null;
            }
            name = ReformatString(name);
            foreach (NetworkUser readOnlyInstances in NetworkUser.readOnlyInstancesList)
            {
                if (ReformatString(readOnlyInstances.userName).StartsWith(name))
                {
                    return readOnlyInstances;
                }
            }
            foreach (NetworkUser readOnlyInstances2 in NetworkUser.readOnlyInstancesList)
            {
                if (ReformatString(readOnlyInstances2.userName).Contains(name))
                {
                    return readOnlyInstances2;
                }
            }
            return null;
        }

        internal static string ReformatString(string input)
        {
            return Regex.Replace(input, "[ '_.,-]", string.Empty).ToLower();
        }

        internal static ItemIndex RandomItem()
        {
            var idx = rng.Next(ItemCatalog.allItems.Count());

            return ItemCatalog.allItems.AsParallel()
                .DefaultIfEmpty(ItemIndex.None)
                .ElementAtOrDefault(idx);
        }

        internal static ItemIndex FindItem(string item, ManualLogSource log)
        {
            return ItemCatalog.allItems.AsParallel()
                .Where((candidate) =>
                {
                    ItemDef LocalizedString = ItemCatalog.GetItemDef(candidate);
                    return Language.GetString(LocalizedString.nameToken).IndexOf(item, System.StringComparison.OrdinalIgnoreCase) >= 0;
                })
                .DefaultIfEmpty(ItemIndex.None)
                .First();
        }
    }
}