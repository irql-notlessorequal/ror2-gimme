using BepInEx;
using R2API;

using RoR2;
using RoR2.UI;

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

using static System.StringComparison;
using static RoR2.RoR2Content;
using BepInEx.Logging;

namespace ExamplePlugin
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
    [BepInPlugin("gimmeguid", "Gimme", "0.0.1")]

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
 //           On.RoR2.UserAchievementManager.GrantAchievement += UserAchievementManager_GrantAchievement;

            log.LogInfo("Gimme loaded successfully.");
        }

        private static void UserAchievementManager_GrantAchievement(AchievementDef achievementDef)
        {
            /* Prevent achievements since this plugin makes it really easy to get them. */
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
                    if (str2.ToUpperInvariant() == "GIVE" || str2.ToUpperInvariant() == "G" || str2.ToUpperInvariant() == "GIVEITEM")
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
                                baseToken = "<color=#AAE6F0>/g itemname playername [amount]\nWill transfer items from sender's inventory into playername's inventory"
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
                    else
                        orig.Invoke(self, sender, concommandName, userArgs);
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

            /*if (inventory1 == inventory2)
                return "<color=#FF8282>Target can not be the sender</color>";*/

            string str1 = "<color=#AAE6F0>" + netUserFromString.masterController.GetDisplayName() + "</color>";
            string str2 = "<color=#AAE6F0>" + sender.masterController.GetDisplayName() + "</color>";
            EquipmentIndex equipmentIndex = EquipmentIndex.None;

            ItemIndex itemIndex = ItemIndex.None;
            if (args[0].ToLower() == "e" || args[0].ToLower() == "equip" || args[0].ToLower() == "equipment")
            {
                equipmentIndex = inventory1.GetEquipmentIndex();
                if (equipmentIndex == EquipmentIndex.None)
                    return "<color=#FF8282>Sender does not have an </color><color=#ff7d00>equipment</color>";
            }

            if (itemIndex == ItemIndex.None)
                itemIndex = StringParsers.FindItem(args[0], log);

            if (itemIndex == ItemIndex.None)
                return "<color=#FF8282>Could not find specified </color>item<color=#FF8282> '<color=#ff4646>" + args[0] + "</color>'</color>";

            ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
            PickupIndex pickupIndex1 = PickupCatalog.FindPickupIndex(itemIndex);
            string coloredString1 = Util.GenerateColoredString(Language.GetString(itemDef.nameToken), PickupCatalog.GetPickupDef(pickupIndex1).baseColor);

            if (itemDef == RoR2Content.Items.CaptainDefenseMatrix || itemDef.tier == ItemTier.NoTier && itemDef != RoR2Content.Items.ExtraLifeConsumed && itemDef != DLC1Content.Items.ExtraLifeVoidConsumed && itemDef != DLC1Content.Items.FragileDamageBonusConsumed && itemDef != DLC1Content.Items.HealingPotionConsumed && itemDef != DLC1Content.Items.RegeneratingScrapConsumed)
                return coloredString1 + "<color=#FF8282> is not dropable</color>";

            int num = 1;

            if (args.Length == 3)
            {
                if (!System.Int32.TryParse(args[2], out num))
                {
                    return "<color=#FF8282>Invalid quantity argument!</color>";
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

        internal static ItemIndex FindItem(string item, ManualLogSource log)
        {
            ItemCatalog.allItems.AsParallel()
                .Where((candidate) =>
                {
                    ItemDef LocalizedString = ItemCatalog.GetItemDef(candidate);
                    return Language.GetString(LocalizedString.nameToken).IndexOf(item, System.StringComparison.OrdinalIgnoreCase) >= 0;
                })
                .FirstOrDefault();

            ItemIndex output = ItemCatalog.FindItemIndex(item);

            if (output == ItemIndex.None)
            {
                List<KeyValuePair<string, ItemIndex>> list = ItemCatalog.itemNameToIndex.ToList();

                for (int num = list.Count - 1; num >= 0; num--)
                {
                    KeyValuePair<string, ItemIndex> dict = list[num];
                    ItemDef LocalizedString = ItemCatalog.GetItemDef(dict.Value);

                    if (Language.GetString(LocalizedString.nameToken).IndexOf(item, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return dict.Value;
                    }
                }
            }

            return output;
        }
    }
}