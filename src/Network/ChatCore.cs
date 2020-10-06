using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

class ChatCore : Core
{
	public new Dictionary<int, ChatClient> Clients = new Dictionary<int, ChatClient>();

	public override bool AttemptConnection(TcpClient client)
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

	public new static void SendTCPData(int toClient, Packet packet)
	{
		try
		{
			packet.WriteLength();
			ChatCore core = (ChatCore)Server.the_core;
			core.Clients[toClient].tcp.SendData(packet);
		}
		catch
		{
			Logger.Syslog($"Failed to send data to client #{toClient}");
		}
	}

	public override void InitializePackets()
	{
		for (int i = 1; i <= Config.MaxPlayers; i++)
			Clients.Add(i, new ChatClient(i));

		main_thread_packets = new Dictionary<int, PacketHandler>()
		{
			{(int)Packet.ClientPackets.pong, ChatHandler.HandleConnect },
		};
	}

	public new static string GetClientIP(int clientId)
	{
		ChatCore core = (ChatCore)Server.the_core;
		return core.Clients[clientId].tcp.socket.Client.RemoteEndPoint.ToString();
	}
}

