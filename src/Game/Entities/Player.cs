using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public class Player
{
    public int session;
    public string name;
    public int pid;
    public int aid;
    public int map;
    public int level;
    public PLAYER_SEXES sex;
    public PLAYER_RACES race;
    public Vector3 pos;
    public int heading;
    public Client client;
    public PlayerStats stats;
    public Inventory inventory { get; private set; }

    public Player(Client _client, int _session, int _pid, int _aid, int _level, PLAYER_SEXES _sex, PLAYER_RACES _race, Vector3 _pos, int _heading, PlayerStats _stats)
    {
        this.client = _client;
        this.session = _session;
        this.pid = _pid;
        this.aid = _aid;
        this.sex = _sex;
        this.race = _race;
        this.stats = _stats;
        this.level = _level;
        this.pos = _pos;
        this.heading = _heading;
    }
    ~Player() { }

    public void AssignInventory(Inventory inv)
    {
        this.inventory = inv;
    }

    public async void Dispose()
    {
        string statsRaw = JsonConvert.SerializeObject(this.stats);
        List<MySqlParameter> dumpParams = new List<MySqlParameter>()
        {
            MySQL_Param.Parameter("?pid", pid),
            MySQL_Param.Parameter("?level", level),
            MySQL_Param.Parameter("?x", this.pos.X.ToString("0.000")),
            MySQL_Param.Parameter("?y", this.pos.Y.ToString("0.000")),
            MySQL_Param.Parameter("?z", this.pos.Z.ToString("0.000")),
            MySQL_Param.Parameter("?h", this.heading),
            MySQL_Param.Parameter("?map", this.map),
        };
        await Server.DB.QueryAsync("UPDATE [[player]].player SET `level`=?level, `x`=?x, `y`=?y, `z`=?z, `h`=?h, `map`=?map WHERE `id`=?pid LIMIT 1", dumpParams);

        inventory.Flush();

        List<MySqlParameter> _params = new List<MySqlParameter>()
        {
            MySQL_Param.Parameter("?session", session),
            MySQL_Param.Parameter("?aid", aid),
            MySQL_Param.Parameter("?pid", pid),
        };
        await Server.DB.QueryAsync("DELETE FROM [[player]].sessions WHERE `session`=?session AND `pid`=?pid AND `aid`=?aid LIMIT 1", _params);
        Logger.Syslog($"Player with session id {session} dumped and destroyed.");
    }

    public void UpdatePosition(Vector3 newPos, int newHeading)
    {
        this.pos = newPos;
        this.heading = newHeading;
    }

    public void UpdateClientInventory()
    {
        using (Packet pck = new Packet((int)Packet.ServerPackets.updateInventory))
        {
            pck.Write(inventory.items.Count);
            for (int i = 0; i < inventory.items.Count; i++)
                pck.Write(inventory.items[i]);
            Core.SendTCPData(client.cid, pck);
        }
    }

    public void UpdateStats()
    {
        float moveSpeed = 1.0f;
        float attackSpeed = 1.0f;
        float pAttack = 1.0f;
        float mAttack = 1.0f;

        for (int i = 0; i < inventory.items.Count; i++)
        {
            if(inventory.items[i].window == Item.WINDOW.EQUIPABLES)
            {
                if (inventory.items[i].data.bonus_type0 == BONUS_TYPE.ATT_SPEED)
                    attackSpeed += inventory.items[i].data.bonus_value0;
                if (inventory.items[i].data.bonus_type0 == BONUS_TYPE.MOVE_SPEED)
                    moveSpeed += inventory.items[i].data.bonus_value0;
                if (inventory.items[i].data.bonus_type0 == BONUS_TYPE.P_ATTACK)
                    pAttack += inventory.items[i].data.bonus_value0;
                if (inventory.items[i].data.bonus_type0 == BONUS_TYPE.M_ATTACK)
                    mAttack += inventory.items[i].data.bonus_value0;

                if (inventory.items[i].data.bonus_type1 == BONUS_TYPE.ATT_SPEED)
                    attackSpeed += inventory.items[i].data.bonus_value1;
                if (inventory.items[i].data.bonus_type1 == BONUS_TYPE.MOVE_SPEED)
                    moveSpeed += inventory.items[i].data.bonus_value1;
                if (inventory.items[i].data.bonus_type1 == BONUS_TYPE.P_ATTACK)
                    pAttack += inventory.items[i].data.bonus_value1;
                if (inventory.items[i].data.bonus_type1 == BONUS_TYPE.M_ATTACK)
                    mAttack += inventory.items[i].data.bonus_value1;

                if (inventory.items[i].data.bonus_type2 == BONUS_TYPE.ATT_SPEED)
                    attackSpeed += inventory.items[i].data.bonus_value2;
                if (inventory.items[i].data.bonus_type2 == BONUS_TYPE.MOVE_SPEED)
                    moveSpeed += inventory.items[i].data.bonus_value2;
                if (inventory.items[i].data.bonus_type2 == BONUS_TYPE.P_ATTACK)
                    pAttack += inventory.items[i].data.bonus_value2;
                if (inventory.items[i].data.bonus_type2 == BONUS_TYPE.M_ATTACK)
                    mAttack += inventory.items[i].data.bonus_value2;

                if (inventory.items[i].data.bonus_type3 == BONUS_TYPE.ATT_SPEED)
                    attackSpeed += inventory.items[i].data.bonus_value3;
                if (inventory.items[i].data.bonus_type3 == BONUS_TYPE.MOVE_SPEED)
                    moveSpeed += inventory.items[i].data.bonus_value3;
                if (inventory.items[i].data.bonus_type3 == BONUS_TYPE.P_ATTACK)
                    pAttack += inventory.items[i].data.bonus_value3;
                if (inventory.items[i].data.bonus_type3 == BONUS_TYPE.M_ATTACK)
                    mAttack += inventory.items[i].data.bonus_value3;

                if (inventory.items[i].data.bonus_type4 == BONUS_TYPE.ATT_SPEED)
                    attackSpeed += inventory.items[i].data.bonus_value4;
                if (inventory.items[i].data.bonus_type4 == BONUS_TYPE.MOVE_SPEED)
                    moveSpeed += inventory.items[i].data.bonus_value4;
                if (inventory.items[i].data.bonus_type4 == BONUS_TYPE.P_ATTACK)
                    pAttack += inventory.items[i].data.bonus_value4;
                if (inventory.items[i].data.bonus_type4 == BONUS_TYPE.M_ATTACK)
                    mAttack += inventory.items[i].data.bonus_value4;

                if (inventory.items[i].data.bonus_type5 == BONUS_TYPE.ATT_SPEED)
                    attackSpeed += inventory.items[i].data.bonus_value5;
                if (inventory.items[i].data.bonus_type5 == BONUS_TYPE.MOVE_SPEED)
                    moveSpeed += inventory.items[i].data.bonus_value5;
                if (inventory.items[i].data.bonus_type5 == BONUS_TYPE.P_ATTACK)
                    pAttack += inventory.items[i].data.bonus_value5;
                if (inventory.items[i].data.bonus_type5 == BONUS_TYPE.M_ATTACK)
                    mAttack += inventory.items[i].data.bonus_value5;
            }
        }

        this.stats.movementSpeed = moveSpeed;
        this.stats.attackSpeed = attackSpeed;
        this.stats.pAttack = pAttack;
        this.stats.mAttack = mAttack;
    }
}
