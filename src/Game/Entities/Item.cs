using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

public class Item
{
    public enum WINDOW
    {
        NONE,
        INVENTORY,
        STORAGE,
        EQUIPABLES
    }

    public enum EQUIPABLES_POSITIONS
    {
        WEAPON = 1,
    }

    public long iid { get; private set; }
    public int ownerPid;
    public int count;
    public WINDOW window;
    public int position;
    public ItemData data;

    public Item(int owner, WINDOW _window, int _pos, ItemData data, long _iid = -1, int _count = -1)
    {
        this.ownerPid = owner;
        this.data = data;
        this.window = _window;
        this.position = _pos;
        if (_count > 0)
            this.count = _count;
        else
            this.count = 1;

        if (_iid == -1)
            createItemInDb();
        else
            this.iid = _iid;
    }

    private void createItemInDb()
    {
        List<MySqlParameter> _params = new List<MySqlParameter>() { 
            MySQL_Param.Parameter("?vnum", this.data.vnum), 
            MySQL_Param.Parameter("?owner", this.ownerPid),
            MySQL_Param.Parameter("?window", this.window.ToString()),
            MySQL_Param.Parameter("?count", this.count),
            MySQL_Param.Parameter("?pos", this.position)
        };
        this.iid = Server.DB.QuerySyncReturnAI("INSERT INTO [[player]].item (`vnum`,`owner`,`window`,`count`,`pos`) VALUES (?vnum,?owner,?window,?count,?pos)", _params);
    }

    private bool isUsable()
    {
        if (this.data.type == ITEM_TYPES.NONE)
            return false;

        return true;
    }

    public void Use()
    {
        if (!isUsable())
            return;

        int cid = Server.the_core.getClientFromPid(ownerPid);
        if (this.data.type == ITEM_TYPES.USE_ITEM)
        {
            Logger.Syslog($"Use item of subtype {this.data.sub_type}");
        }
        else if(this.data.type == ITEM_TYPES.WEAPON || this.data.type == ITEM_TYPES.ARMOR)
        {
            if(this.window == WINDOW.INVENTORY)
                equip();
            else if(this.window == WINDOW.EQUIPABLES)
                dequip();
        }
    }

    private void equip()
    {
        int cid = Server.the_core.getClientFromPid(ownerPid);
        if (Server.the_core.Clients[cid].player.inventory.hasEquipped(this.data.type))
        {
            ChatHandler.sendLocalChatMessage(cid, "You are already equipped with this type of item.");
            return;
        }

        this.window = WINDOW.EQUIPABLES;
        if (this.data.type == ITEM_TYPES.WEAPON)
            this.position = 1;
        if (this.data.type == ITEM_TYPES.ARMOR)
            this.position = 2;

        Server.the_core.Clients[cid].player.inventory.UpdateMatrix();
        Server.the_core.Clients[cid].player.UpdateClientInventory();
        Server.the_core.Clients[cid].player.UpdateStats();
    }

    private void dequip()
    {
        int cid = Server.the_core.getClientFromPid(ownerPid);
        if(!Server.the_core.Clients[cid].player.inventory.canFit(this.data.vnum))
        {
            ChatHandler.sendLocalChatMessage(cid, "You have no free space for this.");
            return;
        }

        this.window = WINDOW.INVENTORY;
        this.position = Server.the_core.Clients[cid].player.inventory.getAppropriateWindowPositionForItem(window, this.data.vnum);
        Server.the_core.Clients[cid].player.inventory.UpdateMatrix();
        Server.the_core.Clients[cid].player.UpdateClientInventory();
        Server.the_core.Clients[cid].player.UpdateStats();
    }

    public async void Flush()
    {
        List<MySqlParameter> countParams = new List<MySqlParameter>() { MySQL_Param.Parameter("?id", iid) };
        DataTable rows = await Server.DB.QueryAsync("SELECT COUNT(*) as count FROM [[player]].item WHERE `id`=?id", countParams);
        Int32.TryParse(rows.Rows[0]["count"].ToString(), out int rCount);
        if (rCount == 0)
        {
            Logger.Syslog("[ERROR] Attempted to flush an item that didn't already exist in the database!");
        }
        else
        {
            List<MySqlParameter> dumpParams = new List<MySqlParameter>()
            {
                MySQL_Param.Parameter("?id", iid),
                MySQL_Param.Parameter("?vnum", data.vnum),
                MySQL_Param.Parameter("?owner", ownerPid),
                MySQL_Param.Parameter("?window", window.ToString()),
                MySQL_Param.Parameter("?count", count),
                MySQL_Param.Parameter("?pos", position),
            };
            await Server.DB.QueryAsync("UPDATE [[player]].item SET `owner`=?owner, `window`=?window, `count`=?count, `pos`=?pos WHERE `id`=?id AND `vnum`=?vnum LIMIT 1", dumpParams);
        }
    }
}
