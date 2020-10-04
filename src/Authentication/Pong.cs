using System;
using System.Collections.Generic;
using System.Text;

class Pong
{
	public static void HandlePong(int fromClient, Packet packet)
	{
		int id = packet.ReadInt();
		int pongLen = packet.ReadInt();
		byte[] pong = packet.ReadBytes(pongLen);
		if (id == fromClient)
		{
			byte[] hashed = Security.Hash("PONG" + fromClient, Security.GetSalt());
			if (Security.Verify(pong, hashed))
			{
				using (Packet newPacket = new Packet((int)Packet.ServerPackets.requestAuth))
				{
					newPacket.Write(fromClient);
					AuthCore.SendTCPData(fromClient, newPacket);
				}
			}
			else
			{
				Logger.Syslog($"Invalid client pong received, disconnecting client #{id}");
				AuthCore.Clients[id].tcp.Disconnect();
			}
		}
	}
}
