using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

class AuthCore
{
    private TcpListener socket;
	public static Dictionary<int, PacketHandler> packet_handlers;
	public delegate void PacketHandler(int fromClient, Packet packet);
	public static Dictionary<int, AuthClient> Clients = new Dictionary<int, AuthClient>();

    public AuthCore()
    {
		InitializePackets();
		socket = new TcpListener(IPAddress.Any, Config.Port);
        socket.Start();
        socket.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
    }

	private void TCPConnectCallback(IAsyncResult ar)
	{
		TcpClient client = socket.EndAcceptTcpClient(ar);
		socket.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
		Logger.Syslog($"Incoming connection from {client.Client.RemoteEndPoint}..");

		for (int i = 1; i <= Config.MaxPlayers; i++)
		{
			if (Clients[i].getTcp().socket == null)
			{
				Clients[i].getTcp().Connect(client);
				return;
			}
		}

		Logger.Syslog($"Server is too busy to listen to {client.Client.RemoteEndPoint}");
	}

	private void InitializePackets()
	{
		for (int i = 1; i <= Config.MaxPlayers; i++)
			Clients.Add(i, new AuthClient(i));

		packet_handlers = new Dictionary<int, PacketHandler>()
		{
			{(int)Packet.ClientPackets.pong, HandlePong },
			{(int)Packet.ClientPackets.authenticate, Authenticate },
			{(int)Packet.ClientPackets.enterMap, EnterMap },
		};
	}

	public static void SendTCPData(int toClient, Packet packet)
	{
		try
		{
			packet.WriteLength();
			Clients[toClient].getTcp().SendData(packet);
		}
		catch
		{
			Logger.Syslog($"Failed to send data to client #{toClient}");
		}
	}

	public static string GetClientIP(int clientId)
	{
		return Clients[clientId].getTcp().socket.Client.RemoteEndPoint.ToString();
	}

	// Actual handling of packets
	private void HandlePong(int fromClient, Packet packet)
	{
		int id = packet.ReadInt();
		int pongLen = packet.ReadInt();
		byte[] pong = packet.ReadBytes(pongLen);
		if (id == fromClient)
		{
			byte[] hashed = Security.Hash("PONG" + fromClient, Security.GetSalt());
			if (Security.Verify(pong, hashed))
			{
				// This client is valid, ask it for the authentication data
				using (Packet newPacket = new Packet((int)Packet.ServerPackets.requestAuth))
				{
					newPacket.Write(fromClient);
					AuthCore.SendTCPData(fromClient, newPacket);
				}
			}
			else
			{
				Logger.Syslog($"Invalid client pong received, disconnecting client #{id}");
				AuthCore.Clients[id].getTcp().Disconnect();
			}
		}
	}

	private async void Authenticate(int fromClient, Packet packet)
	{
		int id = packet.ReadInt();
		string user = packet.ReadString();
		string password = packet.ReadString();
		if (id == fromClient)
		{
			List<MySqlParameter> _params = new List<MySqlParameter>()
			{
				MySQL_Param.Parameter("?login", user),
				MySQL_Param.Parameter("?password", password),
			};
			DataTable result = await Server.DB.QueryAsync("SELECT `id` FROM [[account]].account WHERE `login`=?login AND `password`=?password LIMIT 1", _params);
			if (result.Rows.Count == 0)
				SendAuthFailed(fromClient);

			Int32.TryParse(result.Rows[0]["id"].ToString(), out int aid);
			if (aid <= 0)
				SendAuthFailed(fromClient);

			// Check sessions
			List<MySqlParameter> __params = new List<MySqlParameter>()
			{
				MySQL_Param.Parameter("?aid", aid),
			};
			DataTable rows = await Server.DB.QueryAsync("SELECT COUNT(*) AS `count` FROM [[player]].sessions WHERE `aid`=?aid LIMIT 1", __params);
			Int32.TryParse(rows.Rows[0]["count"].ToString(), out int count);
			if (count == 0)
			{
				// Get characters
				List<MySqlParameter> charParam = new List<MySqlParameter>()
				{
					MySQL_Param.Parameter("?id", aid),
					MySQL_Param.Parameter("?max", 8) // Max characters in account
				};
				DataTable charRows = await Server.DB.QueryAsync("SELECT * FROM [[player]].player WHERE `aid`=?id LIMIT ?max", charParam);

				// Send character data
				string data = "";
				for (int i = 0; i < charRows.Rows.Count; i++)
				{
					string pid = charRows.Rows[i]["id"].ToString();
					string name = charRows.Rows[i]["name"].ToString();
					data += pid + ";" + name + "/end/";
				}

				// Set session id
				Random rnd1 = new Random((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
				Random rnd2 = new Random(rnd1.Next(1, Int32.MaxValue));
				int nSessionId = rnd2.Next(1, Int32.MaxValue);
				Clients[fromClient].setSessionId(nSessionId);

				// Set accound id
				Clients[fromClient].setAID(aid);

				// Create session
				List<MySqlParameter> sessParams = new List<MySqlParameter>()
				{
					MySQL_Param.Parameter("?session", Clients[fromClient].getSessionId()),
					MySQL_Param.Parameter("?pid", Clients[fromClient].getSessionId()), // temporary pid
					MySQL_Param.Parameter("?aid", aid)
				};
				await Server.DB.QueryAsync("INSERT INTO [[player]].sessions (session,pid,aid) VALUES (?session,?pid,?aid)", sessParams);

				// Send it
				using (Packet nPacket = new Packet((int)Packet.ServerPackets.charSelection))
				{
					nPacket.Write(fromClient);
					nPacket.Write(nSessionId);
					nPacket.Write(data);
					AuthCore.SendTCPData(fromClient, nPacket);
				}
			}
			else
			{
				using (Packet newPacket = new Packet((int)Packet.ServerPackets.alreadyConnected))
				{
					newPacket.Write(fromClient);
					AuthCore.SendTCPData(fromClient, newPacket);
				}

				// Check if there's any client connected with this session id, if there is, tell it to disconnect and then delete from db.
				// if there isn't one, delete from database
				bool isOnline = false;
				foreach (KeyValuePair<int, AuthClient> c in Clients)
				{
					if (c.Value.getTcp() != null && c.Value != Clients[fromClient])
					{
						if (c.Value.getTcp().socket != null)
						{
							if (c.Value.getTcp().socket.Connected)
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
						MySQL_Param.Parameter("?id", aid),
					};
					await Server.DB.QueryAsync("DELETE FROM [[player]].sessions WHERE `aid`=?id LIMIT 1", delSess);
				}
			}
		}
	}

	private void SendAuthFailed(int client)
	{
		using (Packet newPacket = new Packet((int)Packet.ServerPackets.authResult))
		{
			newPacket.Write(client);
			newPacket.Write(false);
			AuthCore.SendTCPData(client, newPacket);
		}
	}

	private async void EnterMap(int fromClient, Packet packet)
	{
		int cid = packet.ReadInt();
		int sid = packet.ReadInt();
		int pid = packet.ReadInt();

		if(Security.ValidatePacket(cid, fromClient, sid))
		{
			List<MySqlParameter> _params = new List<MySqlParameter>()
			{
				MySQL_Param.Parameter("?id", pid),
			};
			DataTable result = await Server.DB.QueryAsync("SELECT * FROM [[player]].player WHERE `id`=?id LIMIT 1", _params);

			if (result.Rows.Count == 0)
				Clients[fromClient].getTcp().Disconnect();

			Int32.TryParse(result.Rows[0]["aid"].ToString(), out int aid);
			Int32.TryParse(result.Rows[0]["id"].ToString(), out int _pid);
			if (aid <= 0 || _pid < 0)
				Clients[fromClient].getTcp().Disconnect();

			if (Clients[fromClient].getAID() == aid)
			{
				Int32.TryParse(result.Rows[0]["map"].ToString(), out int map);
				float.TryParse(result.Rows[0]["x"].ToString(), out float x);
				float.TryParse(result.Rows[0]["y"].ToString(), out float y);
				float.TryParse(result.Rows[0]["z"].ToString(), out float z);

				// Get the appropriate server for the client
				foreach (GameServer server in Config.GameServers)
				{
					if (server.maps.Contains(map))
					{
						// Assign a PID to our session entry
						List<MySqlParameter> sParams = new List<MySqlParameter>()
						{
							MySQL_Param.Parameter("?session", Clients[fromClient].getSessionId()),
							MySQL_Param.Parameter("?pid", pid),
							MySQL_Param.Parameter("?aid", aid)
						};
						await Server.DB.QueryAsync("UPDATE [[player]].sessions SET `pid`=?pid WHERE `session`=?session AND `aid`=?aid LIMIT 1", sParams);

						// Send a packet to the client telling it to connect to this game server
						using (Packet nPacket = new Packet((int)Packet.ServerPackets.goToServerAt))
						{
							nPacket.Write(fromClient);
							nPacket.Write(Clients[fromClient].getSessionId());
							nPacket.Write(server.addr);
							nPacket.Write(server.port);
							SendTCPData(fromClient, nPacket);
						}

						Logger.Syslog($"Client #{fromClient} is entering map #{map} ({x},{y},{z}) on the server labeled '{server.label}' with pid #{pid} with a session id of {Clients[fromClient].getSessionId()}...");
						break;
					}
					else
					{
						Logger.Syslog($"[ALERT] Client #{fromClient} attempted to enter a character of pid {pid} on a non existing map #{map} !!!");
						Clients[fromClient].getTcp().Disconnect();
						return;
					}
				}
			}
			else
			{
				Logger.Syslog($"[ALERT] Client #{fromClient} attempted to enter a character of pid {pid} but he doesn't own it! AID missmatch (is #{aid} but we were looking for {Clients[fromClient].getAID()} !!!");
				Clients[fromClient].getTcp().Disconnect();
			}
		}
	}
}