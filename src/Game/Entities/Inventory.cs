using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

public class Inventory
{
    public Player owner { get; private set; }
    public List<Item> items = new List<Item>();
    private int[,] inventory_slots = new int[5, 8];

    public Inventory(Player _owner, Item[] _items)
    {
        this.owner = _owner;
        this.items = new List<Item>(_items);
    }

    public static async Task<Inventory> BuildInventory(Player owner)
    {
        Item[] items = await getPlayerInventoryFromDatabase(owner.pid);
        Inventory nInv = new Inventory(owner, items);
        nInv.updateInventoryMatrix();
        return nInv;
    }

    public void Flush()
    {
        for (int i = 0; i < items.Count; i++)
            items[i].Flush();
    }

    public void updateInventoryMatrix()
    {
        int[,] newInventoryMatrix = new int[inventory_slots.GetLength(0), inventory_slots.GetLength(1)];
        int pos = 1;
        for (int y = 0; y < newInventoryMatrix.GetLength(1); y++)
        {
            for (int x = 0; x < newInventoryMatrix.GetLength(0); x++)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].window != Item.WINDOW.INVENTORY)
                        continue;

                    if(items[i].position == pos)
                    {
                        if (items[i].data.size == 1)
                            newInventoryMatrix[x, y] = 1;
                        else
                        {
                            newInventoryMatrix[x, y] = 1;
                            newInventoryMatrix[x, y + 1] = 1;
                        }

                        continue;
                    }
                }
                pos++;
            }
        }

        inventory_slots = newInventoryMatrix;
    }

    public bool canFit(int vnum)
    {
        int size = Config.Items[vnum].size;
        for (int y = 0; y < inventory_slots.GetLength(1); y++)
        {
            for (int x = 0; x < inventory_slots.GetLength(0); x++)
            {
                if (inventory_slots[x, y] == 0)
                {
                    if (size == 1)
                        return true;

                    if (size == 2)
                    {
                        if (inventory_slots[x, y + 1] == 0)
                            return true;
                    }
                }
            }
        }
        return false;
    }

    public bool canFitAtSlot(long iid, int vnum, int newPos)
    {
        int pos = 1;
        int size = Config.Items[vnum].size;
        for (int y = 0; y < inventory_slots.GetLength(1); y++)
        {
            for (int x = 0; x < inventory_slots.GetLength(0); x++)
            {
                if (pos == newPos)
                {
                    if(inventory_slots[x,y] == 0)
                    {
                        if (size == 1)
                            return true;

                        if (size == 2)
                        {
                            if (inventory_slots[x, y + 1] == 0)
                                return true;
                        }
                    }
                }

                pos++;
            }
        }
        return false;
    }

    public int getAppropriateWindowPositionForItem(Item.WINDOW window, int vnum)
    {
        int pos = 1;
        for (int y = 0; y < inventory_slots.GetLength(1); y++)
        {
            for (int x = 0; x < inventory_slots.GetLength(0); x++)
            {
                if (inventory_slots[x, y] == 0)
                {
                    if (Config.Items[vnum].size == 1)
                        return pos;
                    else
                        if (inventory_slots[x, y + 1] == 0) 
                            return pos;
                }
                pos++;
            }
        }

        return pos;
    }

    private bool slotOccupied(int slot, int size, Item.WINDOW window)
    {
        int pos = 1;
        if(window == Item.WINDOW.INVENTORY)
        {
            for (int y = 0; y < inventory_slots.GetLength(1); y++)
            {
                for (int x = 0; x < inventory_slots.GetLength(0); x++)
                {
                    if(pos == slot)
                    {
                        if (inventory_slots[x, y] == 1)
                            return true;

                        if(size == 2)
                        {
                            if (inventory_slots.GetLength(1) < y + 1)
                                continue;
                            if (inventory_slots[x, y + 1] == 1)
                                return true;
                        }
                    }

                    pos++;
                }
            }
        }
        return false;
    }

    public void AddItemToInventory(int vnum, int count, Item.WINDOW window)
    {
        int newPos = getAppropriateWindowPositionForItem(window, vnum);
        if (!Config.Items[vnum].stacks && count > 1)
            count = 1;

        Item nItem = new Item(owner.pid, window, newPos, Config.Items[vnum], -1, count);
        items.Add(nItem);
        updateInventoryMatrix();
        ChatHandler.sendLocalChatMessage(owner.client.cid, $"You have received x{count} {Config.Items[vnum].name}");
    }

    private static async Task<Item[]> getPlayerInventoryFromDatabase(int pid)
    {
        List<Item> list = new List<Item>();
        List<MySqlParameter> _params = new List<MySqlParameter>()
        {
            MySQL_Param.Parameter("?pid", pid),
            
        };
        DataTable rows = await Server.DB.QueryAsync("SELECT * FROM [[player]].item WHERE `owner`=?pid AND `window`='INVENTORY'", _params);
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

    public void SwapItemPosition(long iid, int newPos, Item.WINDOW window)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if(items[i].iid == iid)
            {
                if(slotOccupied(newPos, items[i].data.size, window)) 
                {
                    Item toMoveItem = items[i];
                    Item itemInTargetSlot = null;
                    for (int x = 0; x < items.Count; x++)
                    {
                        if(items[x].data.size == 2)
                        {
                            if (items[x].position == newPos || items[x].position == (newPos - inventory_slots.GetLength(0)) || items[x].position == (newPos + inventory_slots.GetLength(0)))
                            {
                                itemInTargetSlot = items[x];
                                break;
                            }
                        }
                        else
                        {
                            if (items[x].position == newPos)
                            {
                                itemInTargetSlot = items[x];
                                break;
                            }
                        }
                    }

                    if (itemInTargetSlot == null)
                        return;

                    if (toMoveItem.iid == itemInTargetSlot.iid)
                    {
                        if(items[i].position != newPos)
                            items[i].position = newPos;
                        updateInventoryMatrix();
                        owner.UpdateClientInventory();
                    }
                    else
                    {
                        if(toMoveItem.data.size == itemInTargetSlot.data.size)
                        {
                            itemInTargetSlot.position = items[i].position;
                            items[i].position = newPos;
                            updateInventoryMatrix();
                            owner.UpdateClientInventory();
                        }
                        else
                        {
                            ChatHandler.sendLocalChatMessage(owner.client.cid, "You can't use this item here.");
                            //TODO :: USE THIS FOR ADDING BONUSES TO ITEMS AND STUFF
                        }
                    }
                }
                else
                {
                    items[i].position = newPos;
                    updateInventoryMatrix();
                    owner.UpdateClientInventory();
                }
                return;
            }
        }
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
                    updateInventoryMatrix();
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
                        updateInventoryMatrix();
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
                        updateInventoryMatrix();
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
}
