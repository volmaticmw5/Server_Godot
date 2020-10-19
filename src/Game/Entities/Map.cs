using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

class Map
{
    public int id { get; }
    public string name { get; }

    public Map(int _id, string _name)
    {
        this.id = _id;
        this.name = _name;
    }

    public void Update()
    {
        UpdateClientsOfOtherPlayers();
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
                    otherPlayer.stats);

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

