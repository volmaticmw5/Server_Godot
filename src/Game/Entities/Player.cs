using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

public class Player
{
    public string name;
    public int unusedAttributePoints = 0;
    public PlayerData data;
    public Client client;
    public PlayerStats stats;
    public Inventory inventory { get; private set; }
    private bool warping = false;
    private Item itemEquipped;
    public bool playerChanged = false;

    public Player(Client _client, PlayerData pData, PlayerStats _stats)
    {
        this.client = _client;
        this.data = pData;
        this.stats = _stats;
        this.unusedAttributePoints = calcUnusedAttributePoints();
    }

    ~Player() { }

    public void Update()
    {
        if(isAlive())
        {
            doHealthRegen();
        }

        if(playerChanged)
            UpdatePlayerToClient();
        playerChanged = false;
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
        await inventory.Flush();

        if(!warping)
        {
            List<MySqlParameter> _params = new List<MySqlParameter>()
            {
                MySQL_Param.Parameter("?session", data.sid),
                MySQL_Param.Parameter("?aid", data.aid),
                MySQL_Param.Parameter("?pid", data.pid),
            };
            await Server.DB.QueryAsync("DELETE FROM [[player]].sessions WHERE `session`=?session AND `pid`=?pid AND `aid`=?aid LIMIT 1", _params);
            Logger.Syslog($"Player with session id {data.sid} dumped and destroyed.");
        }
    }

    private async Task<int> flush()
    {
        List<MySqlParameter> dumpParams = new List<MySqlParameter>()
        {
            MySQL_Param.Parameter("?pid", data.pid),
            MySQL_Param.Parameter("?level", data.level),
            MySQL_Param.Parameter("?exp", data.exp),
            MySQL_Param.Parameter("?vit", data.vit),
            MySQL_Param.Parameter("?str", data.str),
            MySQL_Param.Parameter("?int", data._int),
            MySQL_Param.Parameter("?dex", data.dex),
            MySQL_Param.Parameter("?x", this.data.pos.X.ToString("0.000")),
            MySQL_Param.Parameter("?y", this.data.pos.Y.ToString("0.000")),
            MySQL_Param.Parameter("?z", this.data.pos.Z.ToString("0.000")),
            MySQL_Param.Parameter("?h", this.data.heading),
            MySQL_Param.Parameter("?map", this.data.map),
        };
        await Server.DB.QueryAsync("UPDATE [[player]].player SET `level`=?level, `exp`=?exp, `vit`=?vit, `str`=?str, `int`=?int, `dex`=?dex, `x`=?x, `y`=?y, `z`=?z, `h`=?h, `map`=?map WHERE `id`=?pid LIMIT 1", dumpParams);
        return 0;
    }

    public async void Warp(int map)
    {
        warping = true;
        await flush();
        await inventory.Flush();
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
        this.data.hp -= damage;
        if (this.data.hp <= 0)
            die();

        playerChanged = true;
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
        this.data.hp = this.data.maxHp / 4;
        this.data.mana = this.data.maxMana / 4.5f;
    }

    public bool isAlive()
    {
        return this.data.hp > 0;
    }

    public void UpdatePosition(Vector3 newPos, int newHeading, bool attacking)
    {
        this.data.pos = newPos;
        this.data.heading = newHeading;
        this.data.attacking = attacking;
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

    public void UpdatePlayerToClient()
    {
        using (Packet pck = new Packet((int)Packet.ServerPackets.updatePlayer))
        {
            pck.Write(data);
            Core.SendTCPData(client.cid, pck);
        }
    }

    public void UpdateStats()
    {
        float moveSpeed = 1.0f;
        float attackSpeed = 1.0f;
        float pAttack = 1.0f;
        float mAttack = 1.0f;
        float mHp = 10.0f;
        float mMn = 10.0f;
        bool foundWeapon = false;
        playerChanged = true;

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
        this.data.maxHp = mHp;
        this.data.maxMana = mMn;
    }

    public float calcHitDamage(float pDef, float mDef)
    {
        if (this.itemEquipped == null)
            return 0;

        return (itemEquipped.data.pDamage - pDef) + (itemEquipped.data.mDamage - mDef);
    }

    private int calcUnusedAttributePoints()
    {
        int totalAttributes = data.level * 5;
        totalAttributes -= data.vit;
        totalAttributes -= data.str;
        totalAttributes -= data._int;
        totalAttributes -= data.dex;
        return totalAttributes;
    }
}
