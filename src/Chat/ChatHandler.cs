using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

public enum CMD_TYPES
{
	GLOBAL_CHAT = '!',
	TRADE_CHAT = '$',
	GM = '/'
}

public enum GM_CMD_TYPES
{
	ITEM = 'i',
	ANNOUNCE = 'n',
	MOB = 'm',
}

public enum PLAYER_CMD_TYPES
{
	DICE = 'd',
	ANIM = 'a',
}

class ChatHandler
{
    public static async void HandleIncomingMessage(int fromClient, Packet packet)
    {
		int cid = packet.ReadInt();
		int sid = packet.ReadInt();
		string msg = packet.ReadString();
		if (!Security.Validate(cid, fromClient, sid))
			return;

		int pid = await getPidFromSessionId(sid);
		if (pid <= 0) return;
		
		if(isMessageCommand(msg))
		{
			if(isMessageGameMaster(msg))
			{
				if(await Security.isGM(pid))
				{
					processGmCommand(cid, msg);
				}
				else
				{
					sendNotEnoughPermissions(fromClient);
				}
			}
			else
			{
				if(isMessagePlayerdCmd(msg))
				{
					//process player cmd (dice, anim, etc)
				}
				else
				{
					if(isGlobalChat(msg))
					{

					}
					else if(isTradeChat(msg))
					{

					}
					else
					{
						sendCmdUnknown(fromClient);
					}
				}
			}
		}
		else
		{
			// normal chat msg
		}
	}

	private static bool isGlobalChat(string msg)
	{
		if (msg[0] == '!')
			return true;
		return false;
	}

	private static bool isTradeChat(string msg)
	{
		if (msg[0] == '$')
			return true;
		return false;
	}

	private static void processGmCommand(int client, string msg)
	{
		GM_CMD_TYPES type = (GM_CMD_TYPES)msg[1];
		switch(type)
		{
			case GM_CMD_TYPES.ITEM:
				processItemCmd(client, msg);
				break;
		}
	}

	private static void processItemCmd(int client, string msg)
	{
		string[] args = msg.Split(' ');
		if (args[1] == "")
		{
			sendMissingArguments(client, "/i <vnum> <count>");
			return;
		}
		Int32.TryParse(args[1], out int vnum);
		if (!ItemManager.VnumExists(vnum))
		{
			sendInvalidArgument(client, "/i <vnum> <count>");
			return;
		}

		int count = 1;
		if (args.Length > 2)
		{
			Int32.TryParse(args[2], out count);
		}

		int res = ItemManager.AddItemToPlayer(Server.the_core.Clients[client].player, Item.WINDOW.INVENTORY, vnum, count);
		if (res == -1)
			sendLocalChatMessage(client, "You can't carry all of this.");
	}

	private static void sendNotEnoughPermissions(int client)
	{
		using (Packet chatPacket = new Packet((int)Packet.ServerPackets.chatCb))
		{
			chatPacket.Write("You don't have enough permissions to do this.");
			Core.SendTCPData(client, chatPacket);
		}
	}

	private static void sendCmdUnknown(int client)
	{
		using (Packet chatPacket = new Packet((int)Packet.ServerPackets.chatCb))
		{
			chatPacket.Write("This is not a valid command, type /help for a list of commands.");
			Core.SendTCPData(client, chatPacket);
		}
	}

	private static void sendMissingArguments(int client, string structure)
	{
		using (Packet chatPacket = new Packet((int)Packet.ServerPackets.chatCb))
		{
			chatPacket.Write($"Missing command arguments: {structure}");
			Core.SendTCPData(client, chatPacket);
		}
	}

	private static void sendInvalidArgument(int client, string structure)
	{
		using (Packet chatPacket = new Packet((int)Packet.ServerPackets.chatCb))
		{
			chatPacket.Write($"Invalid command arguments: {structure}");
			Core.SendTCPData(client, chatPacket);
		}
	}

	public static void sendLocalChatMessage(int client, string message)
	{
		using (Packet chatPacket = new Packet((int)Packet.ServerPackets.chatCb))
		{
			chatPacket.Write(message);
			Core.SendTCPData(client, chatPacket);
		}
	}

	private static bool isMessageCommand(string msg)
	{
		if(Enum.IsDefined(typeof(CMD_TYPES), (int)msg[0]))
			return true;

		return false;
	}

	private static bool isMessageGameMaster(string msg)
	{
		if (Enum.IsDefined(typeof(GM_CMD_TYPES), (int)msg[1]))
			return true;
		return false;
	}

	private static bool isMessagePlayerdCmd(string msg)
	{
		if (Enum.IsDefined(typeof(PLAYER_CMD_TYPES), (int)msg[1]) && msg[0] == '/')
			return true;
		return false;
	}

	private static async Task<int> getPidFromSessionId(int sid)
	{
		int pid = -1;
		List<MySqlParameter> _params = new List<MySqlParameter>()
		{
			MySQL_Param.Parameter("?session", sid),
		};
		DataTable result = await Server.DB.QueryAsync("SELECT `pid` FROM [[player]].sessions WHERE `session`=?session LIMIT 1", _params);
		if (result.Rows[0] == null)
			return -1;
		Int32.TryParse(result.Rows[0]["pid"].ToString(), out pid);
		return pid;
	}
}
