using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

public class Map
{
    public int id { get; }
    public string name { get; }
    public int width { get; }
    public int height { get; }
    public float[,] heightMap;
    public List<NpcSpawnData> spawnData;
    public List<Mob> mobs = new List<Mob>();

    public Map(int _id, string _name, int _width, int _height)
    {
        this.id = _id;
        this.name = _name;
        this.width = _width;
        this.height = _height;
        heightMap = getMapHeightData();
        spawnData = getSpawnData();

        foreach (NpcSpawnData data in spawnData)
        {
            if (data.type == NpcSpawnData.NPC_SPAWN_TYPE.MOB)
                spawnMob(data.id, data.pos, data.time);
            else if(data.type == NpcSpawnData.NPC_SPAWN_TYPE.GROUP)
                spawnGroup(data.id, data.pos, data.time);
        }
    }

    private void spawnMob(int id, Vector2 pos, float respawn_time)
    {
        MobData data = Config.Mobs[id];
        if (data != null)
        {
            mobs.Add(new Mob(data, this, new Vector3(pos.X, heightMap[(int)pos.X, (int)pos.Y], pos.Y), respawn_time, 0, false));
        }
    }

    private void spawnGroup(int id, Vector2 pos, float respawn_time)
    {
        GroupData data = Config.MobGroups[id];
        if(data != null)
        {
            int gid = generateGroupId();
            for (int i = 0; i < data.mobIds.Length; i++)
            {
                MobData mdata = Config.Mobs[data.mobIds[i]];
                mobs.Add(new Mob(mdata, this, new Vector3(pos.X, heightMap[(int)pos.X, (int)pos.Y], pos.Y), respawn_time, gid, true));
            }
        }
    }

    public Mob[] getMobsInGroup(int group)
    {
        List<Mob> m = new List<Mob>();
        for (int i = 0; i < mobs.Count; i++)
        {
            if ((int)mobs[i].gid == (int)group)
                m.Add(mobs[i]);
        }

        return m.ToArray();
    }

    private int generateGroupId()
    {
        Random rnd = new Random();
        return Math.Abs(this.id + (int)MathHelp.TimestampMiliseconds() + rnd.Next(1, Int32.MaxValue));
    }

    private List<NpcSpawnData> getSpawnData()
    {
        List<NpcSpawnData> data = new List<NpcSpawnData>();
        string raw = File.ReadAllText(Config.LocalePath + $"map/{this.id}/npc");
        if(raw != "")
        {
            string[] lines = raw.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i][0] == '#')
                    continue;

                string[] contents = lines[i].Split('\t');
                if(contents.Length > 0)
                {
                    NpcSpawnData.NPC_SPAWN_TYPE type = NpcSpawnData.NPC_SPAWN_TYPE.MOB;
                    if(contents[0] == "g")
                        type = NpcSpawnData.NPC_SPAWN_TYPE.GROUP;

                    string[] posA = contents[2].Split(',');
                    Vector2 pos = new Vector2(float.Parse(posA[0]), float.Parse(posA[1]));

                    data.Add(new NpcSpawnData(type, Int32.Parse(contents[1]), pos, Int32.Parse(contents[3])));
                }
            }
        }
        return data;
    }

    private float[,] getMapHeightData()
    {
        float[,] dataB = (float[,])DeserializeMapHeightData(Config.LocalePath + $"map/{this.id}/height");
        return dataB;
    }

    private object DeserializeMapHeightData(string path)
    {
        using (Stream stream = File.Open(path, FileMode.Open))
        {
            BinaryFormatter bformatter = new BinaryFormatter();
            return bformatter.Deserialize(stream);
        }
    }

    public void Update()
    {
        UpdateClientsOfOtherPlayers();
        UpdateClientsOfMobs();

        lock(mobs)
        {
            foreach (Mob mob in mobs)
            {
                if (mob != null)
                    mob.Update();
            }
        }
    }

    public void removeFromMobList(int mid)
    {
        lock(mobs)
        {
            for (int i = 0; i < mobs.Count; i++)
            {
                if (mobs[i] != null)
                    if (mobs[i].mid == mid)
                        mobs.RemoveAt(i);
            }
        }
    }

    public Mob getMobByMid(int mid)
    {
        for (int i = 0; i < mobs.Count; i++)
        {
            if (mobs[i].mid == mid)
                return mobs[i];
        }
        return null;
    }

    private void UpdateClientsOfMobs()
    {
        foreach (KeyValuePair<int, Client> client in Server.the_core.Clients)
        {
            if (!isPlayerValid(client.Value))
                continue;

            List<Mob> data_mobs = new List<Mob>();
            lock(mobs)
            {
                for (int i = 0; i < mobs.Count; i++)
                {
                    if (Vector3.Distance(mobs[i].position, client.Value.player.pos) > Config.ViewDistance)
                        continue;

                    data_mobs.Add(mobs[i]);
                }
            }

            using (Packet packet = new Packet((int)Packet.ServerPackets.mobsInMap))
            {
                packet.Write(client.Value.cid);
                packet.Write(data_mobs.Count);
                foreach (Mob data in data_mobs)
                    packet.Write(data);

                Core.SendTCPData(client.Value.cid, packet);
            }
        }
    }

    private void UpdateClientsOfOtherPlayers()
    {
        foreach (KeyValuePair<int, Client> client in Server.the_core.Clients)
        {
            if (!isPlayerValid(client.Value))
                continue;

            List<PlayerData> players = GetOtherPlayersDataNearThisPlayer(client.Value);
            SendThisPlayerOtherPlayersData(client.Value.cid, players.ToArray());
        }
    }

    private List<PlayerData> GetOtherPlayersDataNearThisPlayer(Client targetClient)
    {
        List<PlayerData> data = new List<PlayerData>();
        foreach (KeyValuePair<int, Client> other in Server.the_core.Clients)
        {
            if (!isPlayerValid(other.Value))
                continue;
            if (other.Value.player.pid == targetClient.player.pid)
                continue;

            if (Vector3.Distance(targetClient.player.pos, other.Value.player.pos) < Config.ViewDistance)
            {
                Player otherPlayer = other.Value.player;
                PlayerData otherPlayerData = new PlayerData(
                    otherPlayer.pid, 
                    otherPlayer.aid, 
                    otherPlayer.session, 
                    otherPlayer.name, 
                    otherPlayer.level, 
                    otherPlayer.map, 
                    otherPlayer.sex, 
                    otherPlayer.race, 
                    otherPlayer.pos,
                    otherPlayer.heading,
                    otherPlayer.stats,
                    otherPlayer.attacking,
                    otherPlayer.hp,
                    otherPlayer.mana);

                data.Add(otherPlayerData);
            }
        }

        return data;
    }

    private void SendThisPlayerOtherPlayersData(int cid, PlayerData[] players)
    {
        using (Packet packet = new Packet((int)Packet.ServerPackets.playersInMap))
        {
            packet.Write(cid);
            packet.Write(players.Length);
            foreach (PlayerData data in players)
                packet.Write(data);

            Core.SendTCPData(cid, packet);
        }
    }

    private bool isPlayerValid(Client client)
    {
        if (client.player == null)
            return false;
        if (client.player.map != this.id)
            return false;

        return true;
    }
}

