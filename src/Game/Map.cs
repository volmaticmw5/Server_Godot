using System;
using System.Collections.Generic;
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
            if(client.Value.getPlayer() != null)
            {
                if(client.Value.getPlayer().getMap() == this.id)
                {
                    // Go through all the players on this map, if they're close enough, send data to this client

                }
            }
        }
    }
}

