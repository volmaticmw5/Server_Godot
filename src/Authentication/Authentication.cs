﻿using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Authentication
{
	public static async void Authenticate(int fromClient, Packet packet)
	{
		AuthCore core = (AuthCore)Server.the_core;
		int id = packet.ReadInt();
		string user = packet.ReadString();
		string password = packet.ReadString();

		if (!Security.ReceivedIdMatchesClientId(id, fromClient))
		{
			AuthHelpers.SendAuthFailed(fromClient); 
			return;
		}
			
		int aid = await AuthHelpers.GetAidFromLoginPassword(fromClient, user, password);
		bool hasSession = await AuthHelpers.DoesAidHaveSession(aid);
		if (!hasSession)
		{
			AuthHelpers.SetSessionIDtoClient(fromClient, aid);
			core.Clients[fromClient].setAID(aid);
			AuthHelpers.CreateSessionInDatabase(fromClient, aid);
			CharacterSelectionEntry[] characters = await AuthHelpers.GetCharactersInAccount(aid);
			AuthHelpers.SendCharacterSelectionDataToClient(fromClient, characters);
		}
		else
		{
			core.Clients[fromClient].setAID(aid);
			AuthHelpers.SendAlreadyConnectedPacket(fromClient);
			AuthHelpers.SendDisconnectPacketToAlreadyConnectedClient(fromClient);
		}
	}

	public static async void EnterMap(int fromClient, Packet packet)
	{
		AuthCore core = (AuthCore)Server.the_core;
		int cid = packet.ReadInt();
		int sid = packet.ReadInt();
		int pid = packet.ReadInt();

		if (!Security.Validate(cid, fromClient, sid))
			return;

		bool ownsPlayer = await AuthHelpers.AccountOwnsPlayer(cid, pid);
		if (!ownsPlayer)
		{
			core.Clients[fromClient].tcp.Disconnect();
			return;
		}

		DataTable result = await AuthHelpers.GetPlayerData(pid);
		await AuthHelpers.AssignPidToSession(result, cid);
		AuthHelpers.MakeClientConnectToGameServer(result, cid);
	}
}