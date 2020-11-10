using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

public class Inventory
{
    public Player owner { get; private set; }
    public List<Item> items = new List<Item>();
    private static readonly int inventory_size = 5*7;

    public Inventory(Player _owner, Item[] _items)
    {
        this.owner = _owner;
        this.items = new List<Item>(_items);
    }

    public static async Task<Inventory> BuildInventory(Player owner)
    {
        Item[] items = await getPlayerInventoryFromDatabase(owner.data.pid);
        Inventory nInv = new Inventory(owner, items);
        return nInv;
    }

    public async Task<int> Flush()
    {
        for (int i = 0; i < items.Count; i++)
            _ = await items[i].Flush();
        return 0;
    }

    public bool hasEquipped(ITEM_TYPES type)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].window == Item.WINDOW.EQUIPABLES && items[i].data.type == type)
                return true;
        }
        return false;
    }

    public Item getItemAtPosition(int pos, Item.WINDOW window)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].window == window && items[i].position == pos)
                return items[i];
        }
        return null;
    }

    public int getAppropriateWindowPositionForItem(Item.WINDOW window, int vnum)
    {
        if(window == Item.WINDOW.INVENTORY)
        {
            for (int i = 0; i < inventory_size; i++)
            {
                if (!slotOccupied(i, window))
                    return i+1;
            }
        }

        return 1;
    }

    private bool slotOccupied(int slot, Item.WINDOW window)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].window != window)
                continue;
            if (items[i].position == slot)
                return true;
        }
        return false;
    }

    public void AddItemToInventory(int vnum, int count, Item.WINDOW window)
    {
        int newPos = getAppropriateWindowPositionForItem(window, vnum);
        if (!Config.Items[vnum].stacks && count > 1)
            count = 1;

        Item nItem = new Item(owner.data.pid, window, newPos, Config.Items[vnum], -1, count);
        items.Add(nItem);
        ChatHandler.sendLocalChatMessage(owner.client.cid, $"You have received x{count} {Config.Items[vnum].name}");
    }

    public void AddCountToItem(Item item, int count, Item.WINDOW window)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == item && items[i].window == window) 
            {
                items[i].count += count;
                break;
            }
        }

        ChatHandler.sendLocalChatMessage(owner.client.cid, $"You have received x{count} {item.data.name}");
    }

    public void ChangeItemPosition(long iid, int newPos, Item.WINDOW window)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if(items[i].iid == iid)
            {
                if(window != items[i].window)
                {
                    if (slotOccupied(newPos, window))
                        return;

                    Logger.ItemLog(items[i].data.vnum, iid, $"MOVE,{window.ToString()},{items[i].position},{newPos}");
                    items[i].window = window;
                    items[i].position = newPos;
                    owner.UpdateClientInventory();
                }
                else
                {
                    if (slotOccupied(newPos, window))
                    {
                        Item toMoveItem = items[i];
                        Item itemInTargetSlot = null;
                        for (int x = 0; x < items.Count; x++)
                        {
                            if (items[x].position == newPos)
                            {
                                itemInTargetSlot = items[x];
                                break;
                            }
                        }

                        if (itemInTargetSlot == null)
                            return;

                        if (toMoveItem.iid == itemInTargetSlot.iid)
                        {
                            Logger.Syslog("do nothing?...");
                            /*
                            Logger.ItemLog(items[i].data.vnum, iid, $"MOVE,{window.ToString()},{items[i].position},{newPos}");
                            if (items[i].position != newPos)
                                items[i].position = newPos;
                            owner.UpdateClientInventory();
                            owner.UpdateStats();
                            */
                        }
                        else
                        {
                            Logger.ItemLog(items[i].data.vnum, iid, $"MOVE,{window.ToString()},{items[i].position},{newPos}");
                            itemInTargetSlot.position = items[i].position;
                            items[i].position = newPos;
                            owner.UpdateClientInventory();
                            owner.UpdateStats();
                        }
                    }
                    else
                    {
                        Logger.ItemLog(items[i].data.vnum, iid, $"MOVE,{window.ToString()},{items[i].position},{newPos}");
                        items[i].position = newPos;
                        owner.UpdateClientInventory();
                        owner.UpdateStats();
                    }
                }

                return;
            }
        }
    }

    public bool hasSpaceForItem()
    {
        return (inventory_size - items.Count) > 0;
    }

    public void UseItemAtPosition(int pos, Item.WINDOW window)
    {
        Item targetItem = getItemAtPosition(pos, window);
        if (targetItem == null)
            return;

        if(targetItem.data.type == ITEM_TYPES.NONE)
        {
            ChatHandler.sendLocalChatMessage(owner.client.cid, "This item can't be used.");
            return;
        }

        targetItem.Use();
    }

    public void RemoveItem(long iid, int count)
    {
        List<Item> current = new List<Item>(items);
        for (int i = 0; i < items.Count; i++)
        {
            if(items[i].iid == iid)
            {
                if (items[i].count - count <= 0)
                {
                    items[i].window = Item.WINDOW.NONE;
                    items[i].position = 0;

                    current.Remove(items[i]);
                    items = current;
                    return;
                }
                else
                {
                    items[i].count -= count;
                    return;
                }
            }
        }
    }

    public void RemoveItem(int id, int count)
    {
        List<Item> current = new List<Item>(items);
        int remainer = 0;
        bool altered = false;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].data.vnum == id)
            {
                if(remainer == 0)
                {
                    if (items[i].count - count <= 0)
                    {
                        remainer = count - items[i].count;
                        items[i].window = Item.WINDOW.NONE;
                        items[i].position = 0;
                        current.Remove(items[i]);
                        altered = true;
                        continue;
                    }
                    else
                    {
                        items[i].count -= count;
                        return;
                    }
                }
                else
                {
                    if (items[i].count - remainer <= 0)
                    {
                        items[i].window = Item.WINDOW.NONE;
                        items[i].position = 0;

                        remainer = count - items[i].count;
                        current.Remove(items[i]);
                        altered = true;
                        continue;
                    }
                    else
                    {
                        items[i].count -= remainer;
                        return;
                    }
                }
            }
        }

        if (altered)
            items = current;
    }

    public Item GetItemWithLowestAmountById(int id, Item.WINDOW window)
    {
        int lowest = 999;
        int key = -1;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].data.vnum == id && items[i].window == window)
            {
                if (items[i].count < lowest)
                {
                    lowest = items[i].count;
                    key = i;
                }
            }
        }

        return items[key];
    }

    public bool HasItem(int id)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].data.vnum == id)
                return true;
        }
        return false;
    }

    public bool HasItem(long iid)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].iid == iid)
                return true;
        }
        return false;
    }

    private static async Task<Item[]> getPlayerInventoryFromDatabase(int pid)
    {
        List<Item> list = new List<Item>();
        List<MySqlParameter> _params = new List<MySqlParameter>()
        {
            MySQL_Param.Parameter("?pid", pid),

        };
        DataTable rows = await Server.DB.QueryAsync("SELECT * FROM [[player]].item WHERE `owner`=?pid AND (`window`='INVENTORY' OR `window`='EQUIPABLES')", _params);
        for (int i = 0; i < rows.Rows.Count; i++)
        {
            long.TryParse(rows.Rows[i]["id"].ToString(), out long iid);
            Int32.TryParse(rows.Rows[i]["vnum"].ToString(), out int vnum);
            Item.WINDOW window = (Item.WINDOW)Enum.Parse(typeof(Item.WINDOW), rows.Rows[i]["window"].ToString());
            Int32.TryParse(rows.Rows[i]["count"].ToString(), out int count);
            Int32.TryParse(rows.Rows[i]["pos"].ToString(), out int pos);
            Item nItem = new Item(pid, window, pos, Config.Items[vnum], iid, count);
            list.Add(nItem);
        }
        return list.ToArray();
    }
}
