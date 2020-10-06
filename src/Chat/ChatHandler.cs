using System;
using System.Collections.Generic;
using System.Text;

class ChatHandler
{
    public static void HandleConnect(int fromClient, Packet packet)
    {
        Logger.Syslog($"Client #{fromClient} is now connected to chat server.");
    }
}
