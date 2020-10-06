using System;
using System.Collections.Generic;
using System.Text;

class ChatHandler
{
    public static void HandleConnect(int fromClient, Packet packet)
    {
        Logger.Syslog($"client #{fromClient} connected to chat server");
    }
}
