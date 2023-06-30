using Humanizer;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Principal;
using System.Xml.Schema;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BetterSSC
{
    internal static class PacketID
    {
        public const byte
            ITEM_Inventory = 9,
            ITEM_Armor = 10,
            ITEM_Dye = 11,
            ITEM_MiscDye = 12,
            ITEM_MiscEquips = 13,
            ITEM_Bank = 14,
            ITEM_Bank2 = 15,
            ITEM_Bank3 = 16,
            ITEM_Bank4 = 17,
            STAT_LifeMax = 21,
            STAT_LifeMax2 = 22,
            STAT_ManaMax = 23,
            STAT_ManaMax2 = 24,
            ITEM_TrashItem = 25,
            STAT_CurrentLife = 26,
            STAT_CurrentMana = 27,
            STAT_BiomeTorch = 29,
            STAT_ExtraAccessories = 30;
    }
    public class BetterSSC : Mod
	{
	}
	public class SscPlayer : ModPlayer
	{
        int ItemLen => Total.Length;
        Item[] Total => 
            Player.inventory
                .Concat(Player.armor)
                .Concat(Player.miscEquips)
                .Concat(Player.dye)
                .Concat(Player.miscDyes)
                .Concat(Player.bank.item)
                .Concat(Player.bank2.item)
                .Concat(Player.bank3.item)
                .Concat(Player.bank4.item)
                .Concat(new Item[] { Player.trashItem })
                .ToArray();
        int count         = 0;
        int netMode       = 0;
        TagCompound id    = new TagCompound();
        TagCompound item  = new TagCompound();
        TagCompound stat  = new TagCompound();
        TagCompound sp_i  = new TagCompound();
        TagCompound sp_s  = new TagCompound();
        public override void SaveData(TagCompound tag)
        {
            if (Player.dead) return;
            TagCompound total = new TagCompound()
            { 
                { "count", id.Count },
                { "mode", Main.netMode }
            };
            for (int i = 0; i < id.Count; i++)
            {
                int _id = id.GetInt($"id{i}");
                total.Add($"world_id{i}", _id);
                total.Add($"stat_id{i}", stat.GetCompound($"{_id}_stat"));
            }
            for (int j = 0; j < ItemLen; j++)
            {
                var _item = item.GetCompound($"item{j}");
                total.Add($"item_id{j}", _item);
                if (Main.netMode == 0)
                { 
                    total.Add($"sp_i{j}", _item);
                }
            }
            total.Add("sp_s", sp_s);
            tag.Add($"tag_{Main.worldID}", total);
        }
        public override void LoadData(TagCompound tag)
        {
            TagCompound total = new TagCompound();
            count = tag.GetInt("count");
            netMode = tag.GetInt("mode");
            for (int i = 0; i < count; i++)
            { 
                int _id = tag.GetInt($"world_id{i}");
                id.Set($"id{i}", _id, true);
                total.Set($"tag_{_id}", tag.GetCompound($"tag_{_id}"), true);
                stat.Set($"{_id}_stat", total
                    .GetCompound($"tag_{_id}")
                    .GetCompound($"stat_id{i}"),
                    true);
                for (int j = 0; j < ItemLen; j++)
                {
                    item.Set($"{_id}_item{j}", total
                        .GetCompound($"tag_{_id}")
                        .GetCompound($"item_id{j}"),
                        true);
                    sp_i.Set($"sp_i{j}", tag.GetCompound($"sp_i{j}"), true);
                }
            }
            sp_s = tag.GetCompound("sp_s");
        }
        public override void PreSavePlayer()
        {
            for (int i = 0; i < ItemLen; i++)
            { 
                item.Add($"item{i}", Total[i].SerializeData());
                if (Main.netMode == 0)
                {
                    sp_i.Add($"sp_i{i}", Total[i].SerializeData());
                }
            }
            stat.Add($"{Main.worldID}_stat", new TagCompound()
            { 
                { "statLife", Player.statLife },
                { "statLifeMax", Player.statLifeMax },
                { "statLifeMax2", Player.statLifeMax2 },
                { "statMana", Player.statMana },
                { "statManaMax", Player.statManaMax },
                { "statManaMax2", Player.statManaMax2 },
                { "biomeTorch", Player.unlockedBiomeTorches },
                { "extraAcc", Player.extraAccessory }
            });
            if (Main.netMode == 0)
            {
                sp_s = new TagCompound()
                {
                    { "statLife", Player.statLife },
                    { "statLifeMax", Player.statLifeMax },
                    { "statLifeMax2", Player.statLifeMax2 },
                    { "statMana", Player.statMana },
                    { "statManaMax", Player.statManaMax },
                    { "statManaMax2", Player.statManaMax2 },
                    { "biomeTorch", Player.unlockedBiomeTorches },
                    { "extraAcc", Player.extraAccessory }
                };
            }
        }
        public override void OnEnterWorld(Player player)
        {
            id.Add($"id{id.Count}", Main.worldID);
            
            //item, stat, sp_i, sp_s
            if (Main.netMode == 0 && netMode == 1)
            {
                for (int n = 0; n < sp_i.Count; n++)
                {
                    Item i = Item.DESERIALIZER(sp_i.GetCompound($"sp_i{n}"));
                    HandleInventory(n, player, i);
                }
                HandleStat(player, sp_s);
            }
            else if (Main.netMode == 1)
            {
                for (int n = 0; n < item.Count; n++)
                {
                    Item i = Item.DESERIALIZER(item.GetCompound($"item{n}"));
                    HandleInventory(n, player, i);
                }
                HandleStat(player, stat);
            }
        }

        private void HandleInventory(int i, Player player, Item item)
        {
            int num   = i;
            int num2  = num   - INV.InventoryLen(player);
            int num3  = num2  - INV.ArmorLen(Player);
            int num4  = num3  - INV.MiscEquipsLen(Player);
            int num5  = num4  - INV.DyeLen(Player);
            int num6  = num5  - INV.MiscDyeLen(Player);
            int num7  = num6  - INV.Bank1Len(Player);
            int num8  = num7  - INV.Bank2Len(Player);
            int num9  = num8  - INV.Bank3Len(Player);
            int num10 = num9  - INV.Bank4Len(Player);

            if (num < INV.InventoryLen(player))
            {
                INV.SetInventory(PacketID.ITEM_Inventory, num, item.type, item.stack, item.prefix, player.whoAmI);
            }
            else if (num2 >= 0 && num2 < INV.ArmorLen(Player))
            {
                INV.SetInventory(PacketID.ITEM_Armor, num2, item.type, item.stack, item.prefix, player.whoAmI);
            }
            else if (num3 >= 0 && num3 < INV.MiscEquipsLen(Player))
            {
                INV.SetInventory(PacketID.ITEM_MiscEquips, num3, item.type, item.stack, item.prefix, player.whoAmI);
            }
            else if (num4 >= 0 && num4 < INV.DyeLen(Player))
            {
                INV.SetInventory(PacketID.ITEM_Dye, num4, item.type, item.stack, item.prefix, player.whoAmI);
            }
            else if (num5 >= 0 && num5 < INV.MiscDyeLen(Player))
            {
                INV.SetInventory(PacketID.ITEM_MiscDye, num5, item.type, item.stack, item.prefix, player.whoAmI);
            }
            else if (num6 >= 0 && num6 < INV.Bank1Len(Player))
            {
                INV.SetInventory(PacketID.ITEM_Bank, num6, item.type, item.stack, item.prefix, player.whoAmI);
            }
            else if (num7 >= 0 && num7 < INV.Bank2Len(Player))
            {
                INV.SetInventory(PacketID.ITEM_Bank2, num7, item.type, item.stack, item.prefix, player.whoAmI);
            }
            else if (num8 >= 0 && num8 < INV.Bank3Len(Player))
            {
                INV.SetInventory(PacketID.ITEM_Bank3, num8, item.type, item.stack, item.prefix, player.whoAmI);
            }
            else if (num9 >= 0 && num9 < INV.Bank4Len(Player))
            {
                INV.SetInventory(PacketID.ITEM_Bank4, num9, item.type, item.stack, item.prefix, player.whoAmI);
            }
            else if (num10 >= 0)
            {
                INV.SetInventory(PacketID.ITEM_TrashItem, num10, item.type, item.stack, item.prefix, player.whoAmI);
            }
        }
        private void HandleStat(Player player, TagCompound tag)
        {
            INV.SetHealthMana(PacketID.STAT_CurrentLife, tag.GetInt("statLife"), player.whoAmI);
            INV.SetHealthMana(PacketID.STAT_LifeMax, tag.GetInt("statLifeMax"), player.whoAmI);
            INV.SetHealthMana(PacketID.STAT_LifeMax2, tag.GetInt("statLifeMax2"), player.whoAmI);
            INV.SetHealthMana(PacketID.STAT_CurrentMana, tag.GetInt("statMana"), player.whoAmI);
            INV.SetHealthMana(PacketID.STAT_ManaMax, tag.GetInt("statManaMax"), player.whoAmI);
            INV.SetHealthMana(PacketID.STAT_ManaMax2, tag.GetInt("statManaMax2"), player.whoAmI);
            INV.SetMisc(PacketID.STAT_BiomeTorch, tag.GetBool("biomeTorch"), player.whoAmI);
            INV.SetMisc(PacketID.STAT_ExtraAccessories, tag.GetBool("extraAcc"), player.whoAmI);
        }
    }
    internal class INV
    {
        internal static int InventoryLen(Player player) => player.inventory.Length;
        internal static int ArmorLen(Player player) => player.armor.Length;
        internal static int MiscEquipsLen(Player player) => player.miscEquips.Length;
        internal static int DyeLen(Player player) => player.dye.Length;
        internal static int MiscDyeLen(Player player) => player.miscDyes.Length;
        internal static int Bank1Len(Player player) => player.bank.item.Length;
        internal static int Bank2Len(Player player) => player.bank2.item.Length;
        internal static int Bank3Len(Player player) => player.bank3.item.Length;
        internal static int Bank4Len(Player player) => player.bank4.item.Length;
        internal static int TrashLen() => 1;
        internal static void SetBlankInventory(int whoAmI, bool starting = false)
        {
            Player plr = Main.LocalPlayer;
            SetHealthMana(PacketID.STAT_LifeMax, 100, plr.whoAmI);
            SetHealthMana(PacketID.STAT_LifeMax2, 0, plr.whoAmI);
            SetHealthMana(PacketID.STAT_ManaMax, 20, plr.whoAmI);
            SetHealthMana(PacketID.STAT_ManaMax2, 0, plr.whoAmI);
            SetMisc(PacketID.STAT_BiomeTorch, false, plr.whoAmI);
            SetMisc(PacketID.STAT_ExtraAccessories, false, plr.whoAmI);
            for (int i = 0; i < plr.inventory.Length; i++)
            {
                SetInventory(PacketID.ITEM_Inventory, i, 0, 0, 0, whoAmI);
            }
            //  Default starting items
            if (starting)
            {
                SetInventory(PacketID.ITEM_Inventory, 0, ItemID.CopperShortsword, 1, 0, whoAmI);
                SetInventory(PacketID.ITEM_Inventory, 1, ItemID.CopperPickaxe, 1, 0, whoAmI);
                SetInventory(PacketID.ITEM_Inventory, 2, ItemID.CopperAxe, 1, 0, whoAmI);
            }
            for (int i = 0; i < plr.armor.Length; i++)
            {
                SetInventory(PacketID.ITEM_Armor, i, 0, 0, 0, whoAmI);
            }
            for (int i = 0; i < plr.miscEquips.Length; i++)
            {
                SetInventory(PacketID.ITEM_MiscEquips, i, 0, 0, 0, whoAmI);
            }
            for (int i = 0; i < plr.dye.Length; i++)
            {
                SetInventory(PacketID.ITEM_Dye, i, 0, 0, 0, whoAmI);
            }
            for (int i = 0; i < plr.miscDyes.Length; i++)
            {
                SetInventory(PacketID.ITEM_MiscDye, i, 0, 0, 0, whoAmI);
            }
            for (int i = 0; i < plr.bank.item.Length; i++)
            {
                SetInventory(PacketID.ITEM_Bank, i, 0, 0, 0, whoAmI);
            }
            for (int i = 0; i < plr.bank2.item.Length; i++)
            {
                SetInventory(PacketID.ITEM_Bank2, i, 0, 0, 0, whoAmI);
            }
            for (int i = 0; i < plr.bank3.item.Length; i++)
            {
                SetInventory(PacketID.ITEM_Bank3, i, 0, 0, 0, whoAmI);
            }
            for (int i = 0; i < plr.bank4.item.Length; i++)
            {
                SetInventory(PacketID.ITEM_Bank4, i, 0, 0, 0, whoAmI);
            }
            SetInventory(PacketID.ITEM_TrashItem, 0, 0, 0, 0, whoAmI);
        }
        internal static void SetInventory(byte i, int index, int type, int stack, int prefix, int whoAmi)
        {
            switch (i)
            {
                case PacketID.ITEM_Inventory:
                    if (stack == 0)
                    {
                        Main.player[whoAmi].inventory[index].TurnToAir();
                        break;
                    }
                    Main.player[whoAmi].inventory[index].SetDefaults(type);
                    Main.player[whoAmi].inventory[index].stack = stack;
                    Main.player[whoAmi].inventory[index].prefix = prefix;
                    break;
                case PacketID.ITEM_Dye:
                    if (stack == 0)
                    {
                        Main.player[whoAmi].dye[index].TurnToAir();
                        break;
                    }
                    Main.player[whoAmi].dye[index].SetDefaults(type);
                    Main.player[whoAmi].dye[index].stack = stack;
                    Main.player[whoAmi].dye[index].prefix = prefix;
                    break;
                case PacketID.ITEM_MiscDye:
                    if (stack == 0)
                    {
                        Main.player[whoAmi].miscDyes[index].TurnToAir();
                        break;
                    }
                    Main.player[whoAmi].miscDyes[index].SetDefaults(type);
                    Main.player[whoAmi].miscDyes[index].stack = stack;
                    Main.player[whoAmi].miscDyes[index].prefix = prefix;
                    break;
                case PacketID.ITEM_Armor:
                    if (stack == 0)
                    {
                        Main.player[whoAmi].armor[index].TurnToAir();
                        break;
                    }
                    Main.player[whoAmi].armor[index].SetDefaults(type);
                    Main.player[whoAmi].armor[index].stack = stack;
                    Main.player[whoAmi].armor[index].prefix = prefix;
                    break;
                case PacketID.ITEM_MiscEquips:
                    if (stack == 0)
                    {
                        Main.player[whoAmi].miscEquips[index].TurnToAir();
                        break;
                    }
                    Main.player[whoAmi].miscEquips[index].SetDefaults(type);
                    Main.player[whoAmi].miscEquips[index].stack = stack;
                    Main.player[whoAmi].miscEquips[index].prefix = prefix;
                    break;
                case PacketID.ITEM_Bank:
                    if (stack == 0)
                    {
                        Main.player[whoAmi].bank.item[index].TurnToAir();
                        break;
                    }
                    Main.player[whoAmi].bank.item[index].SetDefaults(type);
                    Main.player[whoAmi].bank.item[index].stack = stack;
                    Main.player[whoAmi].bank.item[index].prefix = prefix;
                    break;
                case PacketID.ITEM_Bank2:
                    if (stack == 0)
                    {
                        Main.player[whoAmi].bank2.item[index].TurnToAir();
                        break;
                    }
                    Main.player[whoAmi].bank2.item[index].SetDefaults(type);
                    Main.player[whoAmi].bank2.item[index].stack = stack;
                    Main.player[whoAmi].bank2.item[index].prefix = prefix;
                    break;
                case PacketID.ITEM_Bank3:
                    if (stack == 0)
                    {
                        Main.player[whoAmi].bank3.item[index].TurnToAir();
                        break;
                    }
                    Main.player[whoAmi].bank3.item[index].SetDefaults(type);
                    Main.player[whoAmi].bank3.item[index].stack = stack;
                    Main.player[whoAmi].bank3.item[index].prefix = prefix;
                    break;
                case PacketID.ITEM_Bank4:
                    if (stack == 0)
                    {
                        Main.player[whoAmi].bank4.item[index].TurnToAir();
                        break;
                    }
                    Main.player[whoAmi].bank4.item[index].SetDefaults(type);
                    Main.player[whoAmi].bank4.item[index].stack = stack;
                    Main.player[whoAmi].bank4.item[index].prefix = prefix;
                    break;
                case PacketID.ITEM_TrashItem:
                    if (stack == 0)
                    {
                        Main.player[whoAmi].trashItem.TurnToAir();
                        break;
                    }
                    Main.player[whoAmi].trashItem.SetDefaults(type);
                    Main.player[whoAmi].trashItem.stack = stack;
                    Main.player[whoAmi].trashItem.prefix = prefix;
                    break;
            }
        }
        internal static void SetHealthMana(byte i, int value, int whoAmi)
        {
            switch (i)
            {
                case PacketID.STAT_LifeMax:
                    Main.player[whoAmi].statLifeMax = value;
                    break;
                case PacketID.STAT_LifeMax2:
                    Main.player[whoAmi].statLifeMax2 = value;
                    break;
                case PacketID.STAT_ManaMax:
                    Main.player[whoAmi].statManaMax = value;
                    break;
                case PacketID.STAT_ManaMax2:
                    Main.player[whoAmi].statManaMax2 = value;
                    break;
                case PacketID.STAT_CurrentLife:
                    Main.player[whoAmi].statLife = value;
                    break;
                case PacketID.STAT_CurrentMana:
                    Main.player[whoAmi].statMana = value;
                    break;
            }
        }
        internal static void SetMisc(byte i, bool flag, int whoAmi)
        {
            switch (i)
            {
                case PacketID.STAT_BiomeTorch:
                    Main.player[whoAmi].unlockedBiomeTorches = flag;
                    break;
                case PacketID.STAT_ExtraAccessories:
                    Main.player[whoAmi].extraAccessory = flag;
                    break;
            }
        }
    }
}