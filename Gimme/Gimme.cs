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
using RoR2;

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
        private const string VERSION = "1.0.3";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles")]
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
                string chatMessage = userArgs?.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(chatMessage) || sender.networkUser == null)
                {
                    orig.Invoke(self, sender, concommandName, userArgs);
                }
                else
                {
                    chatMessage = chatMessage.Trim();
                    if (!chatMessage.StartsWith("/"))
                    {
                        orig.Invoke(self, sender, concommandName, userArgs);
                        return;
                    }

                    string[] source = chatMessage.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (source.Length == 0)
                    {
                        orig.Invoke(self, sender, concommandName, userArgs);
                        return;
                    }
                    string command = source[0].Substring(1);
                    if (string.IsNullOrEmpty(command))
                    {
                        orig.Invoke(self, sender, concommandName, userArgs);
                        return;
                    }
                    string[] arguments = source.Skip(1).ToArray();
                    string upperCommand = command.ToUpperInvariant();
                    if (upperCommand == "GIMME" || upperCommand == "GI")
                    {
                        EchoChat(sender.networkUser, chatMessage);
                        if (arguments.Length == 0 || arguments[0].Equals("HELP", System.StringComparison.OrdinalIgnoreCase))
                        {
                            Reply("<color=#AAE6F0>/gi itemname playername [amount]\n/gimme itemname playername [amount]\n/gr [amount]\n/gimmerandom [amount]\nWill give items into playername's inventory</color>");
                        }
                        else
                        {
                            Reply(GimmeLogic.GiveItem(sender.networkUser, arguments) ?? "<color=#ff4646>ERROR: null output</color>");
                        }
                    }
                    else if (upperCommand == "GR" || upperCommand == "GIMMERANDOM")
                    {
                        EchoChat(sender.networkUser, chatMessage);
                        Reply(GimmeLogic.GiveRandomItem(sender.networkUser, arguments) ?? "<color=#ff4646>ERROR: null output</color>");
                    }
                    else if (upperCommand == "GIMME_DUMP_ITEMS")
                    {
                        GimmeLogic.DumpItems();
                        Reply("Gimme wrote a gimme_items.txt into your game's directory.");
                    }
                    else
                    {
                        orig.Invoke(self, sender, concommandName, userArgs);
                    }
                }
            }
        }

        private static void EchoChat(NetworkUser user, string text)
        {
            Chat.SendBroadcastChat(new Chat.UserChatMessage
            {
                sender = user.gameObject,
                text = text
            });
        }

        private static void Reply(string token)
        {
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = token
            });
        }
    }

    internal class GimmeLogic
    {
        public const string RANDOM_ITEM = "#gimme-random";

        public const string green = "<color=#96EBAA>";
        public const string player = "<color=#AAE6F0>";
        public const string error = "<color=#FF8282>";
        public const string bold = "<color=#ff4646>";

        private static readonly Dictionary<ItemDef, int> RESTRICTED_ITEMS = new Dictionary<ItemDef, int>();

        static GimmeLogic()
        {
            /** Restricted items. */

            /* Too many Shaped Glass will put characters into a respawn loop, which eventually will explode their session */
            RESTRICTED_ITEMS.Add(RoR2Content.Items.LunarDagger, 64);
            /* 1024 Bottled Chaos makes my game lag (on 2017-era hardware) for a while */
            RESTRICTED_ITEMS.Add(DLC1Content.Items.RandomEquipmentTrigger, 128);
            /* Limit movement speed boosts to one hundred, otherwise you will literally hit world bounds instantly */
            RESTRICTED_ITEMS.Add(RoR2Content.Items.SprintBonus, 100);
            RESTRICTED_ITEMS.Add(RoR2Content.Items.Hoof, 100);
            RESTRICTED_ITEMS.Add(DLC1Content.Items.AttackSpeedAndMoveSpeed, 100);
            RESTRICTED_ITEMS.Add(DLC2Content.Items.AttackSpeedPerNearbyAllyOrEnemy, 100);
            RESTRICTED_ITEMS.Add(DLC3Content.Items.CookedSteak, 100);
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
            RESTRICTED_ITEMS.Add(DLC3Content.Items.TransferDebuffOnHit, 16);
            /* Game's HUD does not display more than 255 charges, we already have one by default. */
            RESTRICTED_ITEMS.Add(RoR2Content.Items.EquipmentMagazine, 254);
            /* Too many Irradiant Pearls will boost your movement speed too far */
            RESTRICTED_ITEMS.Add(RoR2Content.Items.ShinyPearl, 100);
            /**
             * Fun story,
             * I tried giving myself 4096 Egocentrism, my FPS dropped down to one.
             */
            RESTRICTED_ITEMS.Add(DLC1Content.Items.LunarSun, 100);
            /**
             * This doesn't scale past one hundred.
             */
            RESTRICTED_ITEMS.Add(DLC2Content.Items.LowerPricedChests, 100);
            /**
             * One hundred is alreay a very long time for a """temporary""" item.
             */
            RESTRICTED_ITEMS.Add(DLC3Content.Items.Duplicator, 100);
        }

        public static string GiveRandomItem(NetworkUser user, string[] args)
        {
            NetworkUser netUserFromString = StringParsers.GetRandomUser();
            Inventory recipient = netUserFromString?.master?.inventory;
            if (!recipient)
                return "<color=#ff4646>ERROR: Unable to get player inventory!</color>";

            int quantity = ParseRandomQuantity(args);

            return ProvideItem(user, recipient, netUserFromString, quantity, RANDOM_ITEM);
        }

        public static string GiveItem(NetworkUser user, string[] args)
        {
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

            Inventory recipient = netUserFromString?.master?.inventory;
            if (!recipient)
                return "<color=#ff4646>Player not found or invalid player name.</color>";

            return ProvideItem(user, recipient, netUserFromString, quantity, item);
        }

        internal static string ProvideItem(NetworkUser sender, Inventory recipientInventory, NetworkUser recipientUser, int num, string item)
        {
            string recipientDisplayName = recipientUser.masterController != null ? recipientUser.masterController.GetDisplayName() : recipientUser.userName;
            string senderDisplayName = sender != null ? (sender.masterController != null ? sender.masterController.GetDisplayName() : sender.userName) : "Unknown";
            string recipientName = "<color=#AAE6F0>" + recipientDisplayName + "</color>";
            string senderName = "<color=#AAE6F0>" + senderDisplayName + "</color>";
            ItemIndex itemIndex = ItemIndex.None;

            /**
             * Limit to 1024 batches, no sane being is going to need more than that.
             */
            if (num < 1)
            {
                num = 1;
            }
            else if (num > 1024)
            {
                num = 1024;
            }

            if (RANDOM_ITEM.Equals(item))
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
                itemIndex = StringParsers.FindItem(item);
            }

            if (itemIndex == ItemIndex.None)
                return "<color=#FF8282>Could not find specified </color>item<color=#FF8282> '<color=#ff4646>" + item + "</color>'</color>";

            if (IsNotSpawnable(itemIndex))
            {
                return "<color=#FF8282>Item cannot be spawned in.</color>";
            }

            ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
            PickupIndex pickupIndex1 = PickupCatalog.FindPickupIndex(itemIndex);
            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex1);
            if (pickupDef == null)
            {
                return "<color=#FF8282>Item cannot be spawned in.</color>";
            }
            string nameToken = Language.GetString(itemDef.nameToken);
            string coloredString1 = Util.GenerateColoredString(nameToken, pickupDef.baseColor);

            if (RESTRICTED_ITEMS.ContainsKey(itemDef))
            {
                /* Check if we are about to give too much to a player and if so, don't do it */
                int currentAmount = recipientInventory.GetItemCountPermanent(itemDef);
                int limit = RESTRICTED_ITEMS[itemDef];

                if (currentAmount + num > limit)
                {
                    return "<color=#FF8282>Player already has too much of item.</color>";
                }
            }

            if (num > 1 && !nameToken.EndsWith("s"))
            {
                coloredString1 += Util.GenerateColoredString("s", pickupDef.baseColor);
            }

            recipientInventory.GiveItemPermanent(itemIndex, num);

            if (ReferenceEquals(recipientUser, sender))
            {
                return $"<color=#96EBAA>{senderName} gave themselves {num} {coloredString1}</color>";
            }
            else
            {
                return $"<color=#96EBAA>{senderName} gave {num} {coloredString1} to </color>{recipientName}";
            }
        }

        internal static bool IsNotSpawnable(ItemIndex index)
        {
            ItemDef itemDefinition = ItemCatalog.GetItemDef(index);
            var localizedName = Language.GetString(itemDefinition.nameToken);

            /**
             * Internal items sadly don't have a stable item index (thanks Randy)
             * 
             * Instead the only real option is to exploit the lack of localization
             * and detect if the item is named in the "ITEM_XXX_NAME" format.
             */
            return localizedName.StartsWith("ITEM_") && localizedName.EndsWith("_NAME");
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

                if (int.TryParse(s, out int parsed))
                {
                    if (quantity == -1)
                    {
                        quantity = parsed;
                    }
                    continue;
                }

                if (netUserFromString == null)
                {
                    netUserFromString = StringParsers.GetNetUserFromString(s);
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

        internal static int ParseRandomQuantity(string[] args)
        {
            if (args != null)
            {
                foreach (string s in args)
                {
                    if (int.TryParse(s, out int parsed))
                    {
                        return parsed;
                    }
                }
            }

            return 1;
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
            try
            {
                using (StreamWriter text = new StreamWriter("gimme_items.txt"))
                {
                    text.WriteLine("// Gimme FORMAT_ONE");
                    foreach (ItemIndex item in ItemCatalog.allItems)
                    {
                        ItemDef itemDefinition = ItemCatalog.GetItemDef(item);
                        var localizedName = Language.GetString(itemDefinition.nameToken);

                        text.WriteLine($"{{index:{(int)item},tier:{(int)itemDefinition.tier},nameToken:\"{Escape(itemDefinition.nameToken)}\",localizedName:\"{Escape(localizedName)}\"}}");
                    }
                }
            }
            catch (System.Exception e) when (e is IOException || e is System.UnauthorizedAccessException)
            {
                Gimme.log?.LogError("[Gimme::DumpItems] Failed to write gimme_items.txt: " + e.Message);
            }
        }

        private static string Escape(string value)
        {
            return value?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? string.Empty;
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
                if (ReformatString(NormalizeString(readOnlyInstances2.userName)).Contains(name))
                {
                    return readOnlyInstances2;
                }
            }
            return null;
        }

        private static readonly Regex ReformatRegex = new Regex("[ '_.,-]", RegexOptions.Compiled);

        internal static string ReformatString(string input)
        {
            return ReformatRegex.Replace(input, string.Empty).ToLowerInvariant();
        }

        internal static string NormalizeString(string input)
        {
            string normalized = input.Normalize(NormalizationForm.FormD);
            StringBuilder stripped = new StringBuilder(normalized.Length);
            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    stripped.Append(c);
                }
            }
            return stripped.ToString();
        }

        internal static ItemIndex RandomItem()
        {
            var idx = rng.Next(ItemCatalog.allItems.Count());

            return ItemCatalog.allItems.AsParallel()
                .DefaultIfEmpty(ItemIndex.None)
                .ElementAtOrDefault(idx);
        }

        internal static ItemIndex FindItem(string item)
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