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
	public static Dictionary<int, PacketHandler> map_thread_packets;
	public delegate void PacketHandler(int fromClient, Packet packet);
	public Dictionary<int, Client> Clients = new Dictionary<int, Client>();

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
			{(int)Packet.ClientPackets.itsme, PlayerManager.NewConnectingPlayer },
		};

		map_thread_packets = new Dictionary<int, PacketHandler>()
		{
			{(int)Packet.ClientPackets.playerBroadcast, PlayerManager.HandlePlayerBroadcast },
		};
	}

	public static void SendTCPData(int toClient, Packet packet)
	{
		try
		{
			packet.WriteLength();
			Server.the_core.Clients[toClient].tcp.SendData(packet);
		}
		catch
		{
			Logger.Syslog($"Failed to send data to client #{toClient}");
		}
	}

	public static string GetClientIP(int clientId)
	{
		return Server.the_core.Clients[clientId].tcp.socket.Client.RemoteEndPoint.ToString();
	}
}
