using System;
using System.Collections.Generic;
using System.Text;

class CorePacket
{
	public Packet packet;

	public CorePacket(byte[] bytes)
	{
		packet = new Packet(bytes);
	}

	public CorePacket()
	{
		packet = new Packet();
	}

	public enum ServerPackets
	{
		identifyoself,
	}

	/// <summary>Sent from client to server.</summary>
	public enum ClientPackets
	{
		itsme,
	}
}
