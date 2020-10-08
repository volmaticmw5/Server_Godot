using System;
using System.Collections.Generic;
using System.Text;

class ChatHandler
{
    public static void HandleConnect(int fromClient, Packet packet)
    {
        Logger.Syslog($"Client #{fromClient} is now connected to chat server.");
    }

	public static void SendChatInfoPacket(int client)
	{
		using (Packet chatPacket = new Packet((int)Packet.ServerPackets.chatInfo))
		{
			chatPacket.Write(Config.ChatAddr);
			chatPacket.Write(Config.ChatPort);
			Core.SendTCPData(client, chatPacket);
		}
	}
}
