using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using System.Text;

class PlayerManager
{
    public static void HandlePositionUpdate(int fromClient, Packet packet)
    {
        int cid = packet.ReadInt();
        int sid = packet.ReadInt();
        Vector3 pos = packet.ReadVector3();
        if (Security.Validate(cid, fromClient, sid))
        {
            Server.the_core.Clients[fromClient].player.UpdatePosition(pos);
        }
    }

    public static async void NewConnectingPlayer(int fromClient, Packet packet)
    {
		int cid = packet.ReadInt();
		int sid = packet.ReadInt();
		if (cid == fromClient)
		{
			// check this session id against the database, if it matches we create a new player instance and so on, if not, we disconnect the client :)
			List<MySqlParameter> _params = new List<MySqlParameter>()
			{
				MySQL_Param.Parameter("?session", sid),
			};

			DataTable result = await Server.DB.QueryAsync("SELECT * FROM [[player]].sessions WHERE `session`=?session LIMIT 1", _params);
			if (result.Rows.Count == 0)
			{
				Server.the_core.Clients[fromClient].tcp.Disconnect(3);
				return;
			}
			Int32.TryParse(result.Rows[0]["pid"].ToString(), out int pid);
			Int32.TryParse(result.Rows[0]["aid"].ToString(), out int aid);
			Int32.TryParse(result.Rows[0]["session"].ToString(), out int session);
			Server.the_core.Clients[fromClient].session_id = session;

			List<MySqlParameter> param = new List<MySqlParameter>()
			{
				MySQL_Param.Parameter("?pid", pid),
			};
			DataTable pResult = await Server.DB.QueryAsync("SELECT * FROM [[player]].player WHERE `id`=?pid LIMIT 1", param);
			Int32.TryParse(pResult.Rows[0]["sex"].ToString(), out int sex);
			Int32.TryParse(pResult.Rows[0]["race"].ToString(), out int race);

			PlayerStats stats;
			string rawStats = pResult.Rows[0]["stats"].ToString();
			if (rawStats == "" || rawStats == null)
			{
				stats = new PlayerStats();
			}
			else
			{
				stats = JsonConvert.DeserializeObject<PlayerStats>(rawStats);
			}


			Player player = new Player(Server.the_core.Clients[fromClient], sid, pid, aid, (Sexes)sex, (Races)race, stats);
			Server.the_core.Clients[fromClient].setPlayer(player);
			List<MySqlParameter> __params = new List<MySqlParameter>()
			{
				MySQL_Param.Parameter("?pid", pid),
				MySQL_Param.Parameter("?aid", aid),
			};
			DataTable rows = await Server.DB.QueryAsync("SELECT * FROM [[player]].player WHERE `id`=?pid AND `aid`=?aid LIMIT 1", __params);

			if (rows.Rows.Count == 0)
			{
				Server.the_core.Clients[fromClient].tcp.Disconnect(4);
				return;
			}

			float.TryParse(rows.Rows[0]["x"].ToString(), out float x);
			float.TryParse(rows.Rows[0]["y"].ToString(), out float y);
			float.TryParse(rows.Rows[0]["z"].ToString(), out float z);
			Int32.TryParse(rows.Rows[0]["map"].ToString(), out int map);

			Server.the_core.Clients[fromClient].player.name = rows.Rows[0]["name"].ToString();
			Server.the_core.Clients[fromClient].player.map = map;
			Server.the_core.Clients[fromClient].player.UpdatePosition(new System.Numerics.Vector3(x, y, z));

			// By now the player has been created, lets tell the client to load target map with target player at target position!
			System.Numerics.Vector3 pos = Server.the_core.Clients[fromClient].player.pos;
			string name = Server.the_core.Clients[fromClient].player.name;
			using (Packet pck = new Packet((int)Packet.ServerPackets.warpTo))
			{
				pck.Write(fromClient); // Cid
				pck.Write(session); // Session id
				pck.Write(map); // map index
				pck.Write(pos); // vec3 pos
				pck.Write(name); // name
				pck.Write(sex); // sex
				pck.Write(race); // race

				// Stats
				pck.Write(stats.movementSpeed);
				pck.Write(stats.attackSpeed);

				Core.SendTCPData(fromClient, pck);
			}
		}
		else
		{
			Server.the_core.Clients[fromClient].tcp.Disconnect(2);
		}
	}
}