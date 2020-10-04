using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;

class Core
{
	public TcpListener socket;
	public static Dictionary<int, PacketHandler> main_thread_packets;
	public delegate void PacketHandler(int fromClient, Packet packet);
	public static Dictionary<int, Client> Clients = new Dictionary<int, Client>();

	public Core()
	{
		InitializePackets();
		socket = new TcpListener(IPAddress.Any, Config.Port);
		socket.Start();
		socket.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
	}

	public void TCPConnectCallback(IAsyncResult ar)
	{
		TcpClient client = socket.EndAcceptTcpClient(ar);
		socket.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
		Logger.Syslog($"Incoming connection from {client.Client.RemoteEndPoint}..");

		if(!AttemptConnection(client))
			Logger.Syslog($"Server is too busy to listen to {client.Client.RemoteEndPoint}");
	}

	public virtual bool AttemptConnection(TcpClient client)
	{
		for (int i = 1; i <= Config.MaxPlayers; i++)
		{
			if (Clients[i].tcp.socket == null)
			{
				Clients[i].tcp.Connect(client);
				return true;
			}
		}
		return false;
	}

	public virtual void InitializePackets()
	{
		for (int i = 1; i <= Config.MaxPlayers; i++)
			Clients.Add(i, new Client(i));

		main_thread_packets = new Dictionary<int, PacketHandler>()
		{
			{(int)Packet.ClientPackets.itsme, ItsMe },
			{(int)Packet.ClientPackets.myPosition, HandlePositionUpdate },
		};
	}

	public static void SendTCPData(int toClient, Packet packet)
	{
		try
		{
			packet.WriteLength();
			Clients[toClient].tcp.SendData(packet);
		}
		catch
		{
			Logger.Syslog($"Failed to send data to client #{toClient}");
		}
	}

	public static string GetClientIP(int clientId)
	{
		return Clients[clientId].tcp.socket.Client.RemoteEndPoint.ToString();
	}

	private async void ItsMe(int fromClient, Packet packet)
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
				Clients[fromClient].tcp.Disconnect(3);
				return;
			}
			Int32.TryParse(result.Rows[0]["pid"].ToString(), out int pid);
			Int32.TryParse(result.Rows[0]["aid"].ToString(), out int aid);
			Int32.TryParse(result.Rows[0]["session"].ToString(), out int session);
			Clients[fromClient].session_id = session;

			List<MySqlParameter> param = new List<MySqlParameter>()
			{
				MySQL_Param.Parameter("?pid", pid),
			};
			DataTable pResult = await Server.DB.QueryAsync("SELECT * FROM [[player]].player WHERE `id`=?pid LIMIT 1", param);
			Int32.TryParse(pResult.Rows[0]["sex"].ToString(), out int sex);
			Int32.TryParse(pResult.Rows[0]["race"].ToString(), out int race);

			PlayerStats stats;
			string rawStats = pResult.Rows[0]["stats"].ToString();
			if(rawStats == "" || rawStats == null)
			{
				stats = new PlayerStats();
			}
			else
			{
				stats = JsonConvert.DeserializeObject<PlayerStats>(rawStats);
			}
			

			Player player = new Player(Clients[fromClient], sid, pid, aid, (Sexes)sex, (Races)race, stats);
			Clients[fromClient].setPlayer(player);
			List<MySqlParameter> __params = new List<MySqlParameter>()
			{
				MySQL_Param.Parameter("?pid", pid),
				MySQL_Param.Parameter("?aid", aid),
			};
			DataTable rows = await Server.DB.QueryAsync("SELECT * FROM [[player]].player WHERE `id`=?pid AND `aid`=?aid LIMIT 1", __params);

			if (rows.Rows.Count == 0)
			{
				Clients[fromClient].tcp.Disconnect(4);
				return;
			}

			float.TryParse(rows.Rows[0]["x"].ToString(), out float x);
			float.TryParse(rows.Rows[0]["y"].ToString(), out float y);
			float.TryParse(rows.Rows[0]["z"].ToString(), out float z);
			Int32.TryParse(rows.Rows[0]["map"].ToString(), out int map);

			Clients[fromClient].player.name = rows.Rows[0]["name"].ToString();
			Clients[fromClient].player.map = map;
			Clients[fromClient].player.pos = new System.Numerics.Vector3(x, y, z);

			// By now the player has been created, lets tell the client to load target map with target player at target position!
			System.Numerics.Vector3 pos = Clients[fromClient].player.pos;
			string name = Clients[fromClient].player.name;
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

				SendTCPData(fromClient, pck);
			}
		}
		else
		{
			Clients[fromClient].tcp.Disconnect(2);
		}
	}

	private static void HandlePositionUpdate(int fromClient, Packet packet)
	{
		int cid = packet.ReadInt();
		int sid = packet.ReadInt();
		Vector3 pos = packet.ReadVector3();
		if(Security.ValidateGamePacket(cid, fromClient, sid))
		{
			Clients[fromClient].player.pos = pos;
		}
	}
}
