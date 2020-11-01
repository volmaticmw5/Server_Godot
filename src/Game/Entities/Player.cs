using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

public class Player
{
    public int session;
    public string name;
    public int pid;
    public int aid;
    public int map;
    public int level;
    public float hp;
    public float mana;
    public PLAYER_SEXES sex;
    public PLAYER_RACES race;
    public Vector3 pos;
    public int heading;
    public Client client;
    public PlayerStats stats;
    public Inventory inventory { get; private set; }
    public bool attacking;
    private bool warping = false;
    private Item itemEquipped;

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
        this.hp = 1000000000f;
        this.mana = 100f;
    }
    ~Player() { }

    public void Update()
    {
        if(isAlive())
        {
            doHealthRegen();
        }
    }

    private void doHealthRegen()
    {
        //this.hp++; // todo
    }

    public void AssignInventory(Inventory inv)
    {
        this.inventory = inv;
    }

    public async void Dispose()
    {
        await flush();
        inventory.Flush();

        if(!warping)
        {
            List<MySqlParameter> _params = new List<MySqlParameter>()
            {
                MySQL_Param.Parameter("?session", session),
                MySQL_Param.Parameter("?aid", aid),
                MySQL_Param.Parameter("?pid", pid),
            };
            await Server.DB.QueryAsync("DELETE FROM [[player]].sessions WHERE `session`=?session AND `pid`=?pid AND `aid`=?aid LIMIT 1", _params);
            Logger.Syslog($"Player with session id {session} dumped and destroyed.");
        }
    }

    private async Task<int> flush()
    {
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
        return 0;
    }

    public async void Warp(int map)
    {
        warping = true;
        inventory.Flush();
        await flush();
        for (int i = 3; i > 0; i--)
        {
            ChatHandler.sendLocalChatMessage(client.cid, $"You will be disconnected in {i} second(s)...");
            await Task.Delay(1000);
        }

        using (Packet pck = new Packet((int)Packet.ServerPackets.reconnectWarp))
        {
            pck.Write(map);
            Core.SendTCPData(client.cid, pck);
        }
    }

    public void receiveDamage(float damage)
    {
        this.hp -= damage;
        if (this.hp <= 0)
            die();

        sendDamageSignalToClient((int)damage);
    }

    private void sendDamageSignalToClient(int dmg)
    {
        using (Packet pck = new Packet((int)Packet.ServerPackets.damageSignal))
        {
            pck.Write(dmg);
            Core.SendTCPData(client.cid, pck);
        }
    }

    private void die()
    {
        //send death status to client
        Logger.Syslog("i ded");
    }

    private void respawn()
    {
        UpdateStats();
        this.hp = this.stats.maxHp / 4;
        this.mana = this.stats.maxMana / 4.5f;
    }

    public bool isAlive()
    {
        return this.hp > 0f;
    }

    public void UpdatePosition(Vector3 newPos, int newHeading, bool attacking)
    {
        this.pos = newPos;
        this.heading = newHeading;
        this.attacking = attacking;
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
        float mHp = 100.0f;
        float mMn = 100.0f;
        bool foundWeapon = false;

        for (int i = 0; i < inventory.items.Count; i++)
        {
            if (inventory.items[i].window == Item.WINDOW.EQUIPABLES && inventory.items[i].data.type == ITEM_TYPES.WEAPON)
            {
                itemEquipped = inventory.items[i];
                foundWeapon = true;
            }

            if (inventory.items[i].window == Item.WINDOW.EQUIPABLES)
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

        if (!foundWeapon)
            this.itemEquipped = null;

        this.stats.movementSpeed = moveSpeed;
        this.stats.attackSpeed = attackSpeed;
        this.stats.pAttack = pAttack;
        this.stats.mAttack = mAttack;
        this.stats.maxHp = mHp;
        this.stats.maxMana = mMn;
    }

    public float calcHitDamage(float pDef, float mDef)
    {
        if (this.itemEquipped == null)
            return 0;

        return (itemEquipped.data.pDamage - pDef) + (itemEquipped.data.mDamage - mDef);
    }
}
