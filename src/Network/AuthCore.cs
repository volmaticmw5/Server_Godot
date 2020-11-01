using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

class AuthCore : Core
{
	public new Dictionary<int, AuthClient> Clients = new Dictionary<int, AuthClient>();

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
			AuthCore core = (AuthCore)Server.the_core;
			core.Clients[toClient].tcp.SendData(packet);
		}
		catch
		{
			Logger.Syserr($"Failed to send data to client #{toClient}");
		}
	}

	public override void InitializePackets()
	{
		for (int i = 1; i <= Config.MaxPlayers; i++)
			Clients.Add(i, new AuthClient(i));

		main_thread_packets = new Dictionary<int, PacketHandler>()
		{
			{(int)Packet.ClientPackets.pong, Pong.HandlePong },
			{(int)Packet.ClientPackets.authenticate, Authentication.Authenticate },
			{(int)Packet.ClientPackets.enterMap, Authentication.EnterMap },
			{(int)Packet.ClientPackets.getTargetGameServerForWarp, Authentication.SendTargetGameServerForWarp },
		};
	}

	public new static string GetClientIP(int clientId)
	{
		AuthCore core = (AuthCore)Server.the_core;
		return core.Clients[clientId].tcp.socket.Client.RemoteEndPoint.ToString();
	}
}

