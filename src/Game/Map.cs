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
        // Send every player on this map a list of player data near them
        foreach (KeyValuePair<int, Client> client in Core.Clients)
        {
            if (client.Value.player == null)
                continue;
            if (client.Value.player.map != this.id)
                continue;

            // Go through all the players on this map, if they're close enough, send data to this client
            string data = "";
            foreach (KeyValuePair<int, Client> other in Core.Clients)
            {
                if (other.Value.player == null)
                    continue;

                if (other.Value.player.pid == client.Value.player.pid)
                    continue;

                if (other.Value.player.map == this.id)
                {
                    if(Vector3.Distance(client.Value.player.pos, other.Value.player.pos) < Config.ViewDistance)
                    {
                        data += $"{other.Value.player.pid};{((int)other.Value.player.race).ToString()};{((int)other.Value.player.sex).ToString()};{other.Value.player.name};{other.Value.player.pos.X.ToString()};{other.Value.player.pos.Y.ToString()};{other.Value.player.pos.Z.ToString()}/end/";
                    }
                }
            }

            // Send the packet even if there's no data since the client will only send us their position upon receiving this!
            using (Packet packet = new Packet((int)Packet.ServerPackets.playersInMap))
            {
                packet.Write(client.Value.cid);
                packet.Write(data);

                Core.SendTCPData(client.Value.cid, packet);
            }
        }
    }
}

