/**
 * MIT License
 * 
 * Copyright (c) 2025 IRQL_NOT_LESS_OR_EQUAL
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using BepInEx;
using BepInEx.Logging;

using R2API;
using R2API.Utils;

using RoR2;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.Networking;

namespace Gimme
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInProcess("Risk of Rain 2.exe")]
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Gimme : BaseUnityPlugin
    {
        private const string GUID = "com.nulldev.ror2.gimme";
        private const string NAME = "Gimme";
        private const string VERSION = "1.0.2";

        internal static ManualLogSource log { get; set; }

        public void Awake()
        {
            log = base.Logger;

            On.RoR2.Console.RunCmd += Console_RunCmd;
            On.RoR2.Achievements.BaseAchievement.Grant += AchievementBlocker;

            log.LogInfo("Gimme loaded successfully.");
        }

        private static void AchievementBlocker(On.RoR2.Achievements.BaseAchievement.orig_Grant orig, RoR2.Achievements.BaseAchievement self)
        {
            log.LogDebug("[Gimme::AchievementBlocker] Preventing the following achievement: " + self);
        }

        private static void Console_RunCmd(On.RoR2.Console.orig_RunCmd orig, RoR2.Console self, RoR2.Console.CmdSender sender, string concommandName, List<string> userArgs)
        {
            if (!NetworkServer.active || Run.instance == null || !concommandName.Equals("say", System.StringComparison.InvariantCultureIgnoreCase))
            {
                orig.Invoke(self, sender, concommandName, userArgs);
            }
            else
            {
                string chatMessage = userArgs.FirstOrDefault<string>();
                if (string.IsNullOrWhiteSpace(chatMessage) || !chatMessage.StartsWith("/"))
                {
                    orig.Invoke(self, sender, concommandName, userArgs);
                }
                else
                {
                    string[] source = chatMessage.Split(' ');
                    string command = ((IEnumerable<string>)source).FirstOrDefault<string>().Substring(1);
                    string[] arguments = ((IEnumerable<string>)source).Skip<string>(1).ToArray<string>();
                    if (command.ToUpperInvariant() == "GIMME" || command.ToUpperInvariant() == "GI")
                    {
                        Chat.SendBroadcastChat((ChatMessageBase)new Chat.UserChatMessage()
                        {
                            sender = ((Component)sender.networkUser).gameObject,
                            text = chatMessage
                        });
                        if (arguments.Length < 1 || arguments[0] == "" || arguments[0].ToUpperInvariant() == "HELP")
                        {
                            Chat.SendBroadcastChat((ChatMessageBase)new Chat.SimpleChatMessage()
                            {
                                baseToken = "<color=#AAE6F0>/gi itemname playername [amount]\n/gimme itemname playername [amount]\n/gr [itemname] [amount]\n/gimmerandom [itemname] [amount]\nWill give items into playername's inventory"
                            });
                        }
                        else
                        {
                            string response = GimmeLogic.GiveItem(sender.networkUser, arguments, log);
                            if (response == null)
                            {
                                Chat.SendBroadcastChat((ChatMessageBase)new Chat.SimpleChatMessage()
                                {
                                    baseToken = "<color=#ff4646>ERROR: null output</color>"
                                });
                            }
                            else
                            {
                                Chat.SendBroadcastChat((ChatMessageBase)new Chat.SimpleChatMessage()
                                {
                                    baseToken = response
                                });
                            }
                        }
                    }
                    else if (command.ToUpperInvariant() == "GR" || command.ToUpperInvariant() == "GIMMERANDOM")
                    {
                        Chat.SendBroadcastChat((ChatMessageBase)new Chat.UserChatMessage()
                        {
                            sender = ((Component)sender.networkUser).gameObject,
                            text = chatMessage
                        });

                        string response = GimmeLogic.GiveRandomItem(sender.networkUser, arguments, log);
                        if (response == null)
                        {
                            Chat.SendBroadcastChat((ChatMessageBase)new Chat.SimpleChatMessage()
                            {
                                baseToken = "<color=#ff4646>ERROR: null output</color>"
                            });
                        }
                        else
                        {
                            Chat.SendBroadcastChat((ChatMessageBase)new Chat.SimpleChatMessage()
                            {
                                baseToken = response
                            });
                        }
                    }
                    else if (command.ToUpperInvariant() == "GIMME_DUMP_ITEMS")
                    {
                        GimmeLogic.DumpItems();
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

    /**
     * These items are internal to the game, you never get to see them in actual play.
     */
    internal enum InternalItemNames
    {
        AA_CANNON = 0,
        AA_ARMOR = 1,
        BOOST_ATTACK_SPEED = 23,
        BOOST_EQUIPMENT_RECHARGE = 25,
        CONVERT_CRIT_CHANCE_TO_CRIT_DAMAGE = 35,
        CRIPPLE_WARD_ON_LEVEL = 37,
        DRIZZLE_PLAYER_HELPER = 47,
        DRONE_WEAPONS_BOOST = 49,
        DRONE_WEAPONS_DISPLAY1 = 50,
        DRONE_WEAPONS_DISPLAY2 = 51,
        EMPOWER_ALWAYS = 53,
        GHOST = 76,
        GUMMY_CLONE_IDENTIFIER = 80,
        HEALTH_DECAY = 88,
        INVADING_DOPPELGANGER = 99,
        LEMURIAN_HARNESS = 106,
        LEVEL_BONUS = 107,
        MAGE_ATTUNEMENT = 120,
        MINION_LEASH = 124,
        MONSOON_PLAYER_HELPER = 128,
        PLANT_ON_HIT = 145,
        PLASMA_CORE = 146,
        TEMPEST_ON_KILL = 188,
        WARCRY_ON_COMBAT = 200
    }

    internal class GimmeLogic
    {
        public const string RANDOM_ITEM = "#gimme-random";

        public const string green = "<color=#96EBAA>";
        public const string player = "<color=#AAE6F0>";
        public const string error = "<color=#FF8282>";
        public const string bold = "<color=#ff4646>";

        private static readonly Dictionary<ItemDef, int> RESTRICTED_ITEMS = new Dictionary<ItemDef, int>();
        private static readonly Dictionary<int, int> ITEM_BLACKLIST = new Dictionary<int, int>();

        static GimmeLogic()
        {
            /** Restricted items. */

            /* Too many Shaped Glass will put characters into a respawn loop, which eventually will explode their session */
            RESTRICTED_ITEMS.Add(RoR2Content.Items.LunarDagger, 64);
            /* 1024 Bottled Chaos makes my game lag (on 2017-era hardware) for a while */
            RESTRICTED_ITEMS.Add(DLC1Content.Items.RandomEquipmentTrigger, 128);
            /* Limit movement speed boosts to one hundred, otherwise you will literally hit world bounds instantly */
            RESTRICTED_ITEMS.Add(DLC1Content.Items.AttackSpeedAndMoveSpeed, 100);
            RESTRICTED_ITEMS.Add(RoR2Content.Items.SprintBonus, 100);
            RESTRICTED_ITEMS.Add(RoR2Content.Items.Hoof, 100);
            /* Limit the amount of Wax Quail's so you don't leave the world bounds */
            RESTRICTED_ITEMS.Add(RoR2Content.Items.JumpBoost, 10);
            /* Limit jump heights with H3AD-5T V2 */
            RESTRICTED_ITEMS.Add(RoR2Content.Items.FallBoots, 10);
            /* Prevent instantaenous equipment spam */
            RESTRICTED_ITEMS.Add(RoR2Content.Items.AutoCastEquipment, 32);
            RESTRICTED_ITEMS.Add(DLC1Content.Items.HalfAttackSpeedHalfCooldowns, 8);
            RESTRICTED_ITEMS.Add(RoR2Content.Items.Talisman, 69);
            /* Prevent literally being unable to move */
            RESTRICTED_ITEMS.Add(DLC1Content.Items.HalfSpeedDoubleHealth, 16);
            /* Game's HUD does not display more than 255 charges, we already have one by default. */
            RESTRICTED_ITEMS.Add(RoR2Content.Items.EquipmentMagazine, 254);
            /* Too many Irradiant Pearls will boost your movement speed too far */
            RESTRICTED_ITEMS.Add(RoR2Content.Items.ShinyPearl, 100);
            /**
             * Fun story,
             * I tried giving myself 4096 Egocentrism, my FPS dropped down to one.
             */
            RESTRICTED_ITEMS.Add(DLC1Content.Items.LunarSun, 100);

            /** Blocked items. */
            /** Vanilla */
            ITEM_BLACKLIST.Add((int)InternalItemNames.AA_CANNON, 0);
            ITEM_BLACKLIST.Add((int)InternalItemNames.AA_ARMOR, 0);
            ITEM_BLACKLIST.Add((int)InternalItemNames.BOOST_ATTACK_SPEED, 0);
            ITEM_BLACKLIST.Add((int)InternalItemNames.BOOST_EQUIPMENT_RECHARGE, 0);
            ITEM_BLACKLIST.Add((int)InternalItemNames.CRIPPLE_WARD_ON_LEVEL, 0);
            ITEM_BLACKLIST.Add((int)InternalItemNames.DRIZZLE_PLAYER_HELPER, 0);
            ITEM_BLACKLIST.Add((int)InternalItemNames.GHOST, 0);
            ITEM_BLACKLIST.Add((int)InternalItemNames.HEALTH_DECAY, 0);
            ITEM_BLACKLIST.Add((int)InternalItemNames.INVADING_DOPPELGANGER, 0);
            ITEM_BLACKLIST.Add((int)InternalItemNames.LEVEL_BONUS, 0);
            ITEM_BLACKLIST.Add((int)InternalItemNames.MONSOON_PLAYER_HELPER, 0);

            /** DLC1 */
            ITEM_BLACKLIST.Add((int)InternalItemNames.CONVERT_CRIT_CHANCE_TO_CRIT_DAMAGE, 0);
            ITEM_BLACKLIST.Add((int)InternalItemNames.DRONE_WEAPONS_BOOST, 0);
            ITEM_BLACKLIST.Add((int)InternalItemNames.DRONE_WEAPONS_DISPLAY1, 0);
            ITEM_BLACKLIST.Add((int)InternalItemNames.DRONE_WEAPONS_DISPLAY2, 0);
            ITEM_BLACKLIST.Add((int)InternalItemNames.GUMMY_CLONE_IDENTIFIER, 0);

            /** DLC2 */
            ITEM_BLACKLIST.Add((int)InternalItemNames.LEMURIAN_HARNESS, 0);

            /** Misc. (has no internal definition) */
            ITEM_BLACKLIST.Add((int)InternalItemNames.EMPOWER_ALWAYS, 0);
            ITEM_BLACKLIST.Add((int)InternalItemNames.MAGE_ATTUNEMENT, 0);
            ITEM_BLACKLIST.Add((int)InternalItemNames.MINION_LEASH, 0);
            ITEM_BLACKLIST.Add((int)InternalItemNames.PLANT_ON_HIT, 0);
            ITEM_BLACKLIST.Add((int)InternalItemNames.PLASMA_CORE, 0);
            ITEM_BLACKLIST.Add((int)InternalItemNames.TEMPEST_ON_KILL, 0);
            ITEM_BLACKLIST.Add((int)InternalItemNames.WARCRY_ON_COMBAT, 0);
        }

        public static string GiveRandomItem(NetworkUser user, string[] args, ManualLogSource log)
        {
            Inventory sender = user != null ? user.master.inventory : (Inventory)null;
            NetworkUser netUserFromString = StringParsers.GetRandomUser();
            Inventory recipient = netUserFromString != null ? netUserFromString.master.inventory : (Inventory)null;
            if (!sender || !recipient)
                return "<color=#ff4646>ERROR: Unable to get player inventory!</color>";

            if (!ParsePlayerArguments(user, args, out _, out _, out int quantity))
            {
                return "<color=#ff4646>ERROR: ParsePlayerArguments() returned FALSE!</color>";
            }

            return ProvideItem(user, sender, recipient, netUserFromString, quantity, RANDOM_ITEM, log);
        }

        public static string GiveItem(NetworkUser user, string[] args, ManualLogSource log)
        {
            Inventory sender = user != null ? user.master.inventory : (Inventory)null;

            if (!ParsePlayerArguments(user, args, out string item, out NetworkUser netUserFromString, out int quantity))
            {
                if (args.Length != 2)
                {
                    return "<color=#FF8282>No player name specified!</color>";
                }
                else
                {
                    return "<color=#FF8282>Could not find specified </color>player<color=#FF8282> '<color=#ff4646>" + args[1] + "</color>'</color>";
                }
            }

            Inventory recipient = netUserFromString != null ? netUserFromString.master.inventory : (Inventory)null;
            if (!sender || !recipient)
                return "<color=#ff4646>Player not found or invalid player name.</color>";

            return ProvideItem(user, sender, recipient, netUserFromString, quantity, item, log);
        }

        internal static string ProvideItem(NetworkUser sender, Inventory inventory1, Inventory inventory2, NetworkUser netUserFromString, int num, string item, ManualLogSource log)
        {
            string recipientName = "<color=#AAE6F0>" + netUserFromString.masterController.GetDisplayName() + "</color>";
            string senderName = "<color=#AAE6F0>" + sender.masterController.GetDisplayName() + "</color>";
            ItemIndex itemIndex = ItemIndex.None;

            if (item.Equals(RANDOM_ITEM))
            {
                /**
                  * Avoid trying to roll a non-droppable item.
                  */
                do
                {
                    itemIndex = StringParsers.RandomItem();
                    if (IsNotSpawnable(itemIndex))
                        continue;

                    break;
                } while (true);
            }
            else
            {
                itemIndex = StringParsers.FindItem(item, log);
            }

            if (itemIndex == ItemIndex.None)
                return "<color=#FF8282>Could not find specified </color>item<color=#FF8282> '<color=#ff4646>" + item + "</color>'</color>";

            ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
            PickupIndex pickupIndex1 = PickupCatalog.FindPickupIndex(itemIndex);
            string nameToken = Language.GetString(itemDef.nameToken);
            string coloredString1 = Util.GenerateColoredString(nameToken, PickupCatalog.GetPickupDef(pickupIndex1).baseColor);

            if (IsNotSpawnable(itemIndex))
            {
                return "<color=#FF8282>Item cannot be spawned in.</color>";
            }

            if (RESTRICTED_ITEMS.ContainsKey(itemDef))
            {
                /* Check if we are about to give too much to a player and if so, don't do it */
                int currentAmount = inventory2.GetItemCount(itemDef);
                int limit = RESTRICTED_ITEMS[itemDef];

                if (num >= limit)
                {
                    return "<color=#FF8282>Too much of an item requested, the limit is '" + limit + "'.</color>";
                }

                /**
                 * This should be "AMOUNT + REQUESTED > LIMIT", not ">="
                 */
                if (currentAmount + num > limit)
                {
                    return "<color=#FF8282>Player already has too much of item.</color>";
                }
            }

            if (num > 1 && !nameToken.EndsWith("s"))
            {
                coloredString1 += Util.GenerateColoredString("s", PickupCatalog.GetPickupDef(pickupIndex1).baseColor);
            }

            /**
             * Limit to 1024 batches, no sane being is going to need more than that.
             */
            if (num > 1024)
            {
                num = 1024;
            }

            inventory2.GiveItem(itemIndex, num);

            if (recipientName.Equals(senderName))
            {
                return string.Format("{0}{1} gave themselves {2} {3}</color>", (object)"<color=#96EBAA>", (object)senderName, (object)num, (object)coloredString1);
            }
            else
            {
                return string.Format("{0}{1} gave {2} {3} to </color>{4}", (object)"<color=#96EBAA>", (object)senderName, (object)num, (object)coloredString1, (object)recipientName);
            }
        }

        internal static bool IsNotSpawnable(ItemIndex index)
        {
            return ITEM_BLACKLIST.ContainsKey((int)index);
        }

        internal static bool ParsePlayerArguments(NetworkUser sender, string[] args,
            out string item, out NetworkUser netUserFromString, out int quantity)
        {
            /* Setup vars. */
            item = null;
            netUserFromString = null;
            quantity = -1;

            if (args == null || args.Length == 0)
            {
                return false;
            }

            foreach (string s in args)
            {
                bool isNumber = int.TryParse(s, out _);

                /**
                 * The item name MUST be first in order.
                 * 
                 * If we add a number check, it will break on specific
                 * items like the "57 Leaf Clover"
                 */
                if (item == null)
                {
                    item = s;
                    continue;
                }

                if (netUserFromString == null && !isNumber)
                {
                    netUserFromString = StringParsers.GetNetUserFromString(s);
                    continue;
                }

                if (quantity == -1 && isNumber)
                {
                    if (int.TryParse(s, out quantity))
                        continue;
                }
            }

            /**
             * If there's only one connection, it's probably a solo lobby.
             */
            if (NetworkUser.readOnlyInstancesList.Count == 1)
                netUserFromString = sender;

            if (quantity == -1)
                quantity = 1;

            return true;
        }

        internal static NetworkUser GetPlayer(NetworkUser sender, string arg)
        {
            /**
             * If there's only one connection, it's probably a solo lobby.
             */
            if (NetworkUser.readOnlyInstancesList.Count == 1)
                return sender;

            return StringParsers.GetNetUserFromString(arg);
        }

        internal static void DumpItems()
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
        private static readonly System.Random rng = new System.Random();

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
                else if (result == 0)
                {
                    return itemAcquisitionOrder[itemAcquisitionOrder.Count - 1];
                }
                else
                {
                    return itemAcquisitionOrder[itemAcquisitionOrder.Count - result];
                }
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

        internal static NetworkUser GetRandomUser()
        {
            return NetworkUser.readOnlyInstancesList[rng.Next(NetworkUser.readOnlyInstancesList.Count)];
        }

        internal static NetworkUser GetNetUserFromString(string name)
        {
            /* First Pass: Direct Match */
            if (int.TryParse(name, out var result))
            {
                if (result < NetworkUser.readOnlyInstancesList.Count && result >= 0)
                {
                    return NetworkUser.readOnlyInstancesList[result];
                }
                return null;
            }
            name = ReformatString(name);
            /* Second Pass */
            foreach (NetworkUser readOnlyInstances in NetworkUser.readOnlyInstancesList)
            {
                if (ReformatString(readOnlyInstances.userName).StartsWith(name))
                {
                    return readOnlyInstances;
                }
            }
            /* Third Pass */
            foreach (NetworkUser readOnlyInstances2 in NetworkUser.readOnlyInstancesList)
            {
                if (ReformatString(readOnlyInstances2.userName).Contains(name))
                {
                    return readOnlyInstances2;
                }
            }
            /* For the most evil people */
            foreach (NetworkUser readOnlyInstances2 in NetworkUser.readOnlyInstancesList)
            {
                if (NormalizeString(readOnlyInstances2.userName).Contains(name))
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

        internal static string NormalizeString(string input)
        {
            return input.Normalize(System.Text.NormalizationForm.FormKD);
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