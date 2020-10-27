using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

class PlayerManager
{
    public static void HandlePlayerBroadcast(int fromClient, Packet packet)
    {
        int cid = packet.ReadInt();
        int sid = packet.ReadInt();
		PlayerData pdata = packet.ReadPlayerData();
		bool attacking = packet.ReadBool();
        
        if (Security.Validate(cid, fromClient, sid))
            Server.the_core.Clients[fromClient].player.UpdatePosition(pdata.pos, pdata.heading, attacking);
    }

    public static async void NewConnectingPlayer(int fromClient, Packet packet)
    {
		int cid = packet.ReadInt();
		int sid = packet.ReadInt();
		if (cid != fromClient)
		{
			Server.the_core.Clients[fromClient].tcp.Disconnect(2);
			return;
		}

		int[] accountData = await AssignTargetSessionToClientAndGetAccountData(cid, sid);
		if (!await CreateAndSetNewPlayerData(fromClient, accountData))
			return;

		PlayerData pdata = await GetPlayerData(fromClient, accountData);
		AssignPlayerDataToClient(fromClient, pdata);
		SendInitializePlayerPacket(fromClient, pdata);
	}

	private static async Task<int[]> AssignTargetSessionToClientAndGetAccountData(int client, int sid)
	{
		List<MySqlParameter> _params = new List<MySqlParameter>()
		{
			MySQL_Param.Parameter("?session", sid),
		};
		DataTable result = await Server.DB.QueryAsync("SELECT * FROM [[player]].sessions WHERE `session`=?session LIMIT 1", _params);

		if (result.Rows.Count == 0)
		{
			Server.the_core.Clients[client].tcp.Disconnect(3);
			return null;
		}

		Int32.TryParse(result.Rows[0]["pid"].ToString(), out int pid);
		Int32.TryParse(result.Rows[0]["aid"].ToString(), out int aid);
		Int32.TryParse(result.Rows[0]["session"].ToString(), out int session);
		Server.the_core.Clients[client].session_id = session;
		return new int[] { pid, aid, session };
	}

	private static async Task<bool> CreateAndSetNewPlayerData(int client, int[] data)
	{
		try
		{
			int pid = data[0];
			int aid = data[1];
			int sid = data[2];

			List<MySqlParameter> param = new List<MySqlParameter>()
			{
				MySQL_Param.Parameter("?pid", pid),
			};
			DataTable pResult = await Server.DB.QueryAsync("SELECT * FROM [[player]].player WHERE `id`=?pid LIMIT 1", param);
			Int32.TryParse(pResult.Rows[0]["sex"].ToString(), out int sex);
			Int32.TryParse(pResult.Rows[0]["race"].ToString(), out int race);
			Int32.TryParse(pResult.Rows[0]["level"].ToString(), out int level);
			float.TryParse(pResult.Rows[0]["x"].ToString(), out float x);
			float.TryParse(pResult.Rows[0]["y"].ToString(), out float y);
			float.TryParse(pResult.Rows[0]["z"].ToString(), out float z);
			Vector3 pos = new Vector3(x, y, z);
			Int32.TryParse(pResult.Rows[0]["h"].ToString(), out int heading);

			PlayerStats stats = new PlayerStats();
			Player player = new Player(Server.the_core.Clients[client], sid, pid, aid, level, (PLAYER_SEXES)sex, (PLAYER_RACES)race, pos, heading, stats);
			Inventory inventory = await Inventory.BuildInventory(player);
			player.AssignInventory(inventory);
			Server.the_core.Clients[client].setPlayer(player);
			return true;
		} catch { return false; }
	}

	private static async Task<PlayerData> GetPlayerData(int client, int[] data)
	{
		int pid = data[0];
		int aid = data[1];
		int sid = data[2];

		List<MySqlParameter> __params = new List<MySqlParameter>()
		{
			MySQL_Param.Parameter("?pid", pid),
			MySQL_Param.Parameter("?aid", aid),
		};
		DataTable rows = await Server.DB.QueryAsync("SELECT * FROM [[player]].player WHERE `id`=?pid AND `aid`=?aid LIMIT 1", __params);

		if (rows.Rows.Count == 0)
		{
			Server.the_core.Clients[client].tcp.Disconnect(4);
			return null;
		}

		string name = rows.Rows[0]["name"].ToString();
		float.TryParse(rows.Rows[0]["x"].ToString(), out float x);
		float.TryParse(rows.Rows[0]["y"].ToString(), out float y);
		float.TryParse(rows.Rows[0]["z"].ToString(), out float z);
		Int32.TryParse(rows.Rows[0]["h"].ToString(), out int heading);
		Int32.TryParse(rows.Rows[0]["map"].ToString(), out int map);
		Int32.TryParse(rows.Rows[0]["sex"].ToString(), out int sex);
		Int32.TryParse(rows.Rows[0]["race"].ToString(), out int race);
		Int32.TryParse(rows.Rows[0]["level"].ToString(), out int level);

		PlayerStats stats = new PlayerStats();
		PlayerData nData = new PlayerData(pid, aid, sid, name, level, map, (PLAYER_SEXES)sex, (PLAYER_RACES)race, new Vector3(x, y, z), heading, stats, false);
		return nData;
	}

	private static void AssignPlayerDataToClient(int client, PlayerData data)
	{
		Server.the_core.Clients[client].player.name = data.name;
		Server.the_core.Clients[client].player.map = data.map;
		Server.the_core.Clients[client].player.UpdatePosition(data.pos, data.heading, false);
	}

	private static void SendInitializePlayerPacket(int client, PlayerData data)
	{
		using (Packet pck = new Packet((int)Packet.ServerPackets.warpTo))
		{
			pck.Write(data);
			Core.SendTCPData(client, pck);
		}
	}

	public static void PlayerInstancedSignal(int fromClient, Packet packet)
	{
		int cid = packet.ReadInt();
		int sid = packet.ReadInt();
		if (!Security.Validate(cid, fromClient, sid))
			return;

		Server.the_core.Clients[fromClient].player.UpdateClientInventory();
		Server.the_core.Clients[fromClient].player.UpdateStats();
	}
}