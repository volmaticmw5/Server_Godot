using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

class AuthPacket
{
	public Packet packet;

	public AuthPacket(byte[] bytes)
	{
		packet = new Packet(bytes);
	}

	public AuthPacket()
	{
		packet = new Packet();
	}

	public enum ServerPackets
	{
		connectSucess,
		requestAuth,
		authResult,
		charSelection
	}

	/// <summary>Sent from client to server.</summary>
	public enum ClientPackets
	{
		pong,
		authenticate,
		enterMap
	}
}
