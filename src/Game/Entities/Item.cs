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
        STORAGE
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
