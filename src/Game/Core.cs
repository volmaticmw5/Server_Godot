using System;
using System.Collections.Generic;
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
			{(int)CorePacket.ClientPackets.itsme, ItsMe },
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

		// check this session id against the database, if it matches we create a new player instance and so on, if not, we disconnect the client :)
	}
}
