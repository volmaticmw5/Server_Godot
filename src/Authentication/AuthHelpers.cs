using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class AuthHelpers
{
	public static async Task<int> GetAidFromLoginPassword(int fromClient, string user, string password)
	{
		List<MySqlParameter> _params = new List<MySqlParameter>()
			{
				MySQL_Param.Parameter("?login", user),
				MySQL_Param.Parameter("?password", password),
			};
		DataTable result = await Server.DB.QueryAsync("SELECT `id` FROM [[account]].account WHERE `login`=?login AND `password`=?password LIMIT 1", _params);
		if (result.Rows.Count == 0)
		{
			SendAuthFailed(fromClient);
			return -1;
		}

		Int32.TryParse(result.Rows[0]["id"].ToString(), out int aid);
		if (aid <= 0)
		{
			SendAuthFailed(fromClient);
			return -1;
		}

		return aid;
	}

	public static async Task<bool> DoesAidHaveSession(int aid)
	{
		List<MySqlParameter> __params = new List<MySqlParameter>()
		{
			MySQL_Param.Parameter("?aid", aid),
		};
		DataTable rows = await Server.DB.QueryAsync("SELECT COUNT(*) AS `count` FROM [[player]].sessions WHERE `aid`=?aid LIMIT 1", __params);
		Int32.TryParse(rows.Rows[0]["count"].ToString(), out int count);

		if (count > 0) 
			return true;
		else
			return false;
	}

	public static async Task<CharacterSelectionEntry[]> GetCharactersInAccount(int aid)
	{
		List<MySqlParameter> charParam = new List<MySqlParameter>()
		{
			MySQL_Param.Parameter("?id", aid),
			MySQL_Param.Parameter("?max", Config.MaxCharactersInAccount)
		};
		DataTable charRows = await Server.DB.QueryAsync("SELECT * FROM [[player]].player WHERE `aid`=?id LIMIT ?max", charParam);

		List<CharacterSelectionEntry> characters = new List<CharacterSelectionEntry>();
		for (int i = 0; i < charRows.Rows.Count; i++)
		{
			Int32.TryParse(charRows.Rows[i]["id"].ToString(), out int pid);
			string name = charRows.Rows[i]["name"].ToString();
			characters.Add(new CharacterSelectionEntry(pid, name));
		}

		characters = fillMissingCharacterSlots(characters);
		return characters.ToArray();
	}

	public static void SetSessionIDtoClient(int client, int aid)
	{
		AuthCore core = (AuthCore)Server.the_core;
		Random rnd1 = new Random(MathHelp.TimestampSeconds());
		Random rnd2 = new Random(rnd1.Next(1, Int32.MaxValue) + aid);
		int nSessionId = rnd2.Next(1, Int32.MaxValue);
		core.Clients[client].setSessionId(nSessionId);
	}

	public static async void CreateSessionInDatabase(int client, int aid)
	{
		AuthCore core = (AuthCore)Server.the_core;
		List<MySqlParameter> sessParams = new List<MySqlParameter>()
		{
			MySQL_Param.Parameter("?session", core.Clients[client].session_id),
			MySQL_Param.Parameter("?pid", core.Clients[client].session_id), // as a temporary pid
			MySQL_Param.Parameter("?aid", aid)
		};
		await Server.DB.QueryAsync("INSERT INTO [[player]].sessions (session,pid,aid) VALUES (?session,?pid,?aid)", sessParams);
	}

	public static void SendCharacterSelectionDataToClient(int client, CharacterSelectionEntry[] characters)
	{
		AuthCore core = (AuthCore)Server.the_core;
		using (Packet nPacket = new Packet((int)Packet.ServerPackets.charSelection))
		{
			nPacket.Write(client);
			nPacket.Write(core.Clients[client].session_id);
			for (int i = 0; i < Config.MaxCharactersInAccount; i++)
				nPacket.Write(characters[i]);

			AuthCore.SendTCPData(client, nPacket);
		}
	}

	public static void SendAuthFailed(int client)
	{
		using (Packet newPacket = new Packet((int)Packet.ServerPackets.authResult))
		{
			newPacket.Write(client);
			newPacket.Write(false);
			AuthCore.SendTCPData(client, newPacket);
		}
	}

	public static void SendAlreadyConnectedPacket(int client)
	{
		using (Packet newPacket = new Packet((int)Packet.ServerPackets.alreadyConnected))
		{
			newPacket.Write(client);
			AuthCore.SendTCPData(client, newPacket);
		}
	}

	public static async void SendDisconnectPacketToAlreadyConnectedClient(int client)
	{
		AuthCore core = (AuthCore)Server.the_core;
		bool isOnline = false;
		foreach (KeyValuePair<int, AuthClient> cc in core.Clients)
		{
			if (cc.Value.tcp != null && cc.Value != core.Clients[client])
			{
				if (cc.Value.tcp.socket != null)
				{
					if (cc.Value.tcp.socket.Connected)
					{
						isOnline = true;
						// send a disconnect packet

						break;
					}
				}
			}
		}

		if (!isOnline)
		{
			List<MySqlParameter> delSess = new List<MySqlParameter>()
			{
				MySQL_Param.Parameter("?id", core.Clients[client].aid),
			};
			await Server.DB.QueryAsync("DELETE FROM [[player]].sessions WHERE `aid`=?id LIMIT 1", delSess);
		}
	}

	public static async Task<bool> AccountOwnsPlayer(int cid, int pid)
	{
		AuthCore core = (AuthCore)Server.the_core;
		List<MySqlParameter> _params = new List<MySqlParameter>()
		{
			MySQL_Param.Parameter("?id", pid),
		};
		DataTable result = await Server.DB.QueryAsync("SELECT * FROM [[player]].player WHERE `id`=?id LIMIT 1", _params);

		if (result.Rows.Count == 0)
			return false;

		Int32.TryParse(result.Rows[0]["aid"].ToString(), out int aid);
		Int32.TryParse(result.Rows[0]["id"].ToString(), out int _pid);
		if (aid <= 0 || _pid < 0)
			return false;

		if (core.Clients[cid].aid != aid)
			return false;

		return true;
	}

	public static async Task<DataTable> GetPlayerData(int pid)
	{
		List<MySqlParameter> _params = new List<MySqlParameter>()
		{
			MySQL_Param.Parameter("?id", pid),
		};
		DataTable result = await Server.DB.QueryAsync("SELECT * FROM [[player]].player WHERE `id`=?id LIMIT 1", _params);
		return result;
	}

	public static async Task<int> AssignPidToSession(DataTable rows, int cid)
	{
		AuthCore core = (AuthCore)Server.the_core;
		List<MySqlParameter> sParams = new List<MySqlParameter>()
		{
			MySQL_Param.Parameter("?session", core.Clients[cid].session_id),
			MySQL_Param.Parameter("?pid", rows.Rows[0]["id"].ToString()),
			MySQL_Param.Parameter("?aid", rows.Rows[0]["aid"].ToString())
		};
		await Server.DB.QueryAsync("UPDATE [[player]].sessions SET `pid`=?pid WHERE `session`=?session AND `aid`=?aid LIMIT 1", sParams);
		return 1;
	}

	public static void MakeClientConnectToGameServer(DataTable result, int cid, int forcedMap = -1)
	{
		AuthCore core = (AuthCore)Server.the_core;
		Int32.TryParse(result.Rows[0]["map"].ToString(), out int map);
		Int32.TryParse(result.Rows[0]["id"].ToString(), out int pid);
		if (forcedMap > 0)
			map = forcedMap;

		foreach (GameServer server in Config.GameServers)
		{
			if (server.maps.Contains(map))
			{
				using (Packet nPacket = new Packet((int)Packet.ServerPackets.goToServerAt))
				{
					nPacket.Write(cid);
					nPacket.Write(core.Clients[cid].session_id);
					nPacket.Write(server.addr);
					nPacket.Write(server.port);
					AuthCore.SendTCPData(cid, nPacket);
				}

				Logger.Syslog($"Client #{cid} is entering map #{map} on the server labeled '{server.label}' with pid #{pid} with a session id of {((AuthCore)Server.the_core).Clients[cid].session_id}...");
				break;
			}
			else
			{
				Logger.Syserr($"Client #{cid} attempted to enter a character of pid {pid} on a non existing map #{map} !!!");
				core.Clients[cid].tcp.Disconnect();
				return;
			}
		}
	}

	private static List<CharacterSelectionEntry> fillMissingCharacterSlots(List<CharacterSelectionEntry> characters)
	{
		for (int i = 0; i < Config.MaxCharactersInAccount; i++)
		{
			if(i >= characters.Count)
				characters.Add(new CharacterSelectionEntry(-1, ""));
		}

		return characters;
	}
}