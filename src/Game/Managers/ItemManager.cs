using System;
using System.Collections.Generic;
using System.Text;

public class ItemManager
{
    public static async void FlushItems()
    {
        for (int i = 0; i < Server.the_core.Clients.Count; i++)
        {
            if(Server.the_core.Clients[i].player != null)
                _ = await Server.the_core.Clients[i].player.inventory.Flush();
        }
    }

    public static bool VnumExists(int vnum)
    {
        for (int i = 1; i < Config.Items.Length; i++)
        {
            if (Config.Items[i] == null)
                continue;
            if (Config.Items[i].vnum == vnum)
                return true;
        }

        return false;
    }

    public static int AddItemToPlayer(Player player, Item.WINDOW window, int vnum, int count)
    {
        if (count <= 0)
            count = 1;
        if (count > Config.MaxStackItems)
            count = Config.MaxStackItems;

        if(Config.Items[vnum].stacks)
        {
            if (!player.inventory.canFit(vnum))
                return -1;

            if (!player.inventory.HasItem(vnum))
                player.inventory.AddItemToInventory(vnum, count, window);
            else
            {
                if (player.inventory.GetItemWithLowestAmountById(vnum, window).count + count > Config.MaxStackItems)
                {
                    int current = Config.MaxStackItems - player.inventory.GetItemWithLowestAmountById(vnum, window).count;
                    player.inventory.AddCountToItem(player.inventory.GetItemWithLowestAmountById(vnum, window), current, window);
                    player.inventory.AddItemToInventory(vnum, count - current, window);
                }
                else
                    player.inventory.AddCountToItem(player.inventory.GetItemWithLowestAmountById(vnum, window), count, window);
            }
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                if (!player.inventory.canFit(vnum))
                    return -1;
                player.inventory.AddItemToInventory(vnum, 1, window);
            }
        }

        player.UpdateClientInventory();
        player.UpdateStats();
        return 0;
    }

    public static void ItemUse(int fromClient, Packet packet)
    {
        int cid = packet.ReadInt();
        int sid = packet.ReadInt();
        int pos = packet.ReadInt();
        int window = packet.ReadInt();
        if (!Security.Validate(cid, fromClient, sid))
            return;

        Server.the_core.Clients[cid].player.inventory.UseItemAtPosition(pos, (Item.WINDOW)window);
    }

    public static void ChangeItemPosition(int fromClient, Packet packet)
    {
        int cid = packet.ReadInt();
        int sid = packet.ReadInt();
        long iid = packet.ReadLong();
        int newPos = packet.ReadInt();
        Item.WINDOW window = (Item.WINDOW)packet.ReadInt();
        if (!Security.Validate(cid, fromClient, sid))
            return;
        Server.the_core.Clients[cid].player.inventory.SwapItemPosition(iid, newPos, window);
    }

    public static void RemoveItemFromPlayer(Player player, long iid, int count)
    {
        if (player.inventory.HasItem(iid)){
            player.inventory.RemoveItem(iid, count);
        }
        player.UpdateClientInventory();
        player.UpdateStats();
    }

    public static void RemoveItemFromPlayer(Player player, int id, int count)
    {
        if (player.inventory.HasItem(id))
        {
            player.inventory.RemoveItem(id, count);
        }
        player.UpdateClientInventory();
        player.UpdateStats();
    }

    public static void HandleWeaponHit(int fromClient, Packet packet)
    {
        int cid = packet.ReadInt();
        int sid = packet.ReadInt();
        int mid = packet.ReadInt();
        if (!Security.Validate(cid, fromClient, sid))
            return;

        Map targetMap = MapManager.getMapById(Server.the_core.Clients[cid].player.map);
        Mob targetMob = targetMap.getMobByMid(mid);
        if (targetMob == null)
            return;
        float damage = Server.the_core.Clients[cid].player.calcHitDamage(targetMob.data.stats.pDefense, targetMob.data.stats.mDefense);
        targetMob.receiveDamage(Server.the_core.Clients[cid].player, damage);
    }
}