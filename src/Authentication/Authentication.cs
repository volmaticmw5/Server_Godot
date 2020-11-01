using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Authentication
{
	public static async void Authenticate(int fromClient, Packet packet)
	{
		AuthCore core = (AuthCore)Server.the_core;
		int id = packet.ReadInt();
		string user = packet.ReadString();
		string password = packet.ReadString();

		if (!Security.ReceivedIdMatchesClientId(id, fromClient))
		{
			AuthHelpers.SendAuthFailed(fromClient); 
			return;
		}
			
		int aid = await AuthHelpers.GetAidFromLoginPassword(fromClient, user, password);
		bool hasSession = await AuthHelpers.DoesAidHaveSession(aid);
		if (!hasSession)
		{
			AuthHelpers.SetSessionIDtoClient(fromClient, aid);
			core.Clients[fromClient].setAID(aid);
			AuthHelpers.CreateSessionInDatabase(fromClient, aid);
			CharacterSelectionEntry[] characters = await AuthHelpers.GetCharactersInAccount(aid);
			AuthHelpers.SendCharacterSelectionDataToClient(fromClient, characters);
		}
		else
		{
			core.Clients[fromClient].setAID(aid);
			AuthHelpers.SendAlreadyConnectedPacket(fromClient);
			AuthHelpers.SendDisconnectPacketToAlreadyConnectedClient(fromClient);
		}
	}

	public static async void EnterMap(int fromClient, Packet packet)
	{
		AuthCore core = (AuthCore)Server.the_core;
		int cid = packet.ReadInt();
		int sid = packet.ReadInt();
		int pid = packet.ReadInt();

		if (!Security.Validate(cid, fromClient, sid))
			return;

		bool ownsPlayer = await AuthHelpers.AccountOwnsPlayer(cid, pid);
		if (!ownsPlayer)
		{
			core.Clients[fromClient].tcp.Disconnect();
			return;
		}

		DataTable result = await AuthHelpers.GetPlayerData(pid);
		await AuthHelpers.AssignPidToSession(result, cid);
		AuthHelpers.MakeClientConnectToGameServer(result, cid);
	}

	public static async void SendTargetGameServerForWarp(int fromClient, Packet packet)
	{
		AuthCore core = (AuthCore)Server.the_core;
		int cid = packet.ReadInt();
		int sid = packet.ReadInt();
		int pid = packet.ReadInt();
		int map = packet.ReadInt();

		if (!Security.ReceivedIdMatchesClientId(cid, fromClient))
		{
			AuthHelpers.SendAuthFailed(fromClient);
			return;
		}

		List<MySqlParameter> _params = new List<MySqlParameter>()
		{
			MySQL_Param.Parameter("?session", sid),
			MySQL_Param.Parameter("?pid", pid),
		};
		DataTable rows = await Server.DB.QueryAsync("SELECT COUNT(*) AS count FROM [[player]].sessions WHERE `session`=?session AND `pid`=?pid LIMIT 1", _params);
		if(Int32.Parse(rows.Rows[0]["count"].ToString()) > 0)
		{
			DataTable result = await AuthHelpers.GetPlayerData(pid);
			List<MySqlParameter> mapParams = new List<MySqlParameter>()
			{
				MySQL_Param.Parameter("?map", map),
				MySQL_Param.Parameter("?x", Config.SpawnPositionsForMaps[map].X),
				MySQL_Param.Parameter("?y", Config.SpawnPositionsForMaps[map].Y),
				MySQL_Param.Parameter("?z", Config.SpawnPositionsForMaps[map].Z),
				MySQL_Param.Parameter("?pid", pid),
			};
			await Server.DB.QueryAsync("UPDATE [[player]].player SET `map`=?map, `x`=?x, `y`=?y, `z`=?z WHERE `id`=?pid LIMIT 1", mapParams);
			((AuthCore)Server.the_core).Clients[cid].session_id = sid;
			AuthHelpers.MakeClientConnectToGameServer(result, cid, map);
		}
		else
		{
			Logger.Syserr($"Player #{pid} attempted to enter map #{map} but the session missmatched (sid of {sid})");
			// todo : send a disconnect packet to the client
		}
	}
}
