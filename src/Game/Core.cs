using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Core
{
	private TcpListener socket;
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
			Clients.Add(i, new Client(i));

		main_thread_packets = new Dictionary<int, PacketHandler>()
		{
			{(int)Packet.ClientPackets.itsme, ItsMe },
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

	private void ItsMe(int fromClient, Packet packet)
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
			Server.DB.AddQueryToQeue("SELECT * FROM [[player]].sessions WHERE `session`=?session LIMIT 1", _params, fromClient, HandleItsMe);
		}
		else
		{
			Clients[fromClient].getTcp().Disconnect(2);
		}
		
	}

	private void HandleItsMe(int client, DataTable result)
	{
		if(result.Rows.Count == 0)
		{
			Clients[client].getTcp().Disconnect(3);
			return;
		}

		Int32.TryParse(result.Rows[0]["session"].ToString(), out int sid);
		Int32.TryParse(result.Rows[0]["pid"].ToString(), out int pid);
		Int32.TryParse(result.Rows[0]["aid"].ToString(), out int aid);

		Player player = new Player(Clients[client], sid, pid, aid);
		Clients[client].setPlayer(player);
		List<MySqlParameter> _params = new List<MySqlParameter>()
			{
				MySQL_Param.Parameter("?pid", pid),
				MySQL_Param.Parameter("?aid", aid),
			};
		Server.DB.AddQueryToQeue("SELECT * FROM [[player]].player WHERE `id`=?pid AND `aid`=?aid LIMIT 1", _params, client, HandleCharResult);
	}

	private void HandleCharResult(int client, DataTable result)
	{
		if (result.Rows.Count == 0)
		{
			Clients[client].getTcp().Disconnect(4);
			return;
		}

		float.TryParse(result.Rows[0]["x"].ToString(), out float x);
		float.TryParse(result.Rows[0]["y"].ToString(), out float y);
		float.TryParse(result.Rows[0]["z"].ToString(), out float z);
		Int32.TryParse(result.Rows[0]["map"].ToString(), out int map);

		Clients[client].getPlayer().setName(result.Rows[0]["name"].ToString());
		Clients[client].getPlayer().setMap(map);
		Clients[client].getPlayer().setPos(new System.Numerics.Vector3(x, y, z));

		// By now the player has been created, lets tell the client to load target map with target player at target position!
		System.Numerics.Vector3 pos = Clients[client].getPlayer().getPos();
		string name = Clients[client].getPlayer().getName();
		using (Packet pck = new Packet((int)Packet.ServerPackets.warpTo))
		{
			pck.Write(client); // Cid
			pck.Write(map); // map index
			pck.Write(pos); // vec3 pos
			pck.Write(name); // name

			SendTCPData(client, pck);
		}
	}
}
