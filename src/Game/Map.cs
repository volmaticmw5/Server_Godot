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
            if (client.Value.getPlayer() == null)
                continue;
            if (client.Value.getPlayer().map != this.id)
                continue;

            // Go through all the players on this map, if they're close enough, send data to this client
            string data = "";
            foreach (KeyValuePair<int, Client> other in Core.Clients)
            {
                if (other.Value.getPlayer() == null)
                    continue;

                if (other.Value.getPlayer().pid == client.Value.getPlayer().pid)
                    continue;

                if (other.Value.getPlayer().map == this.id)
                {
                    if(Vector3.Distance(client.Value.getPlayer().pos, other.Value.getPlayer().pos) < Config.ViewDistance)
                    {
                        data += $"{other.Value.getPlayer().pid};{((int)other.Value.getPlayer().race).ToString()};{((int)other.Value.getPlayer().sex).ToString()};{other.Value.getPlayer().name};{other.Value.getPlayer().pos.X.ToString()};{other.Value.getPlayer().pos.Y.ToString()};{other.Value.getPlayer().pos.Z.ToString()}/end/";
                    }
                }
            }

            // Send the packet even if there's no data since the client will only send us their position upon receiving this!
            using (Packet packet = new Packet((int)Packet.ServerPackets.playersInMap))
            {
                packet.Write(client.Value.getClientId()); // Cid
                packet.Write(data);

                Core.SendTCPData(client.Value.getClientId(), packet);
            }
        }
    }
}

