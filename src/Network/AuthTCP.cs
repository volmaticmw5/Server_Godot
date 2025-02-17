﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

class AuthTCP : TCP
{
    public new AuthClient client;

    public AuthTCP(AuthClient _client, int _cid) : base(_client, _cid)
    {
        this.client = _client;
        this.cid = _cid;
    }

    public override void Connect(TcpClient _socket)
    {
        configureSocket(_socket);
        stream.BeginRead(receivedBuff, 0, buffer_size, ReceiveCallback, null);

        /*
         *
         * A new client has connected to the authentication server
         * This is NOT an authentication request, its just a heartbeat 
         * If we hear back a valid PONG response then we "allow" the client to request authentication
         */
        using (Packet newPacket = new Packet((int)Packet.ServerPackets.connectSucess))
        {
            newPacket.Write("Ping?");
            newPacket.Write(cid);
            AuthCore.SendTCPData(cid, newPacket);
        }
        try { Logger.Syslog($"Client #{client.cid} ({AuthCore.GetClientIP(cid)}) connected to the authentication server"); } catch { Logger.Syslog("A client connected to the authentication server but we couldn't retrieve it's ip address."); }
    }

    public override void SendData(Packet packet)
    {
        try
        {
            if (socket != null)
            {
                try
                {
                    stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            Logger.Syserr($"Error sending data to the client {cid}: {ex}");
        }
    }

    public override void ReceiveCallback(IAsyncResult ar)
    {
        AuthCore core = (AuthCore)Server.the_core;
        try
        {
            int byteLength = stream.EndRead(ar);
            if (byteLength <= 0)
            {
                core.Clients[cid].tcp.Disconnect();
                return;
            }

            resetSocket(byteLength);

            stream.BeginRead(receivedBuff, 0, buffer_size, ReceiveCallback, null);
        }
        catch
        {
            core.Clients[cid].tcp.Disconnect();
        }
    }

    public override bool HandleData(byte[] data)
    {
        int packetLength = 0;
        receivedPacket.SetBytes(data);
        if (receivedPacket.UnreadLength() >= 4)
        {
            packetLength = receivedPacket.ReadInt();
            if (packetLength <= 0)
                return true;
        }

        while (packetLength > 0 && packetLength <= receivedPacket.UnreadLength())
        {
            byte[] packetBytes = receivedPacket.ReadBytes(packetLength);

            Packet authPacket = new Packet(packetBytes);
            int packetId = authPacket.ReadInt();

            if (AuthCore.main_thread_packets.ContainsKey(packetId))
            {
                Server.main_thread_manager.addToQeue(() =>
                {
                    AuthCore.main_thread_packets[packetId](cid, authPacket);
                });
            }
            else
            {
                Logger.Syserr($"Received an unknown packet with id of {packetId}");
            }


            packetLength = 0;
            if (receivedPacket.UnreadLength() >= 4)
            {
                packetLength = receivedPacket.ReadInt();
                if (packetLength <= 0)
                    return true;
            }
        }

        if (packetLength <= 1)
            return true;

        return false;
    }

    public void Disconnect()
    {
        try
        {
            Logger.Syslog($"Client #{client.cid} disconnected ({client.tcp.socket.Client.RemoteEndPoint.ToString()})");
            client.setSessionId(-1);
            client.setAID(-1);
        }
        catch { Logger.Syserr($"Failed to properly disconnect client #{client.cid}"); }

        if (socket != null)
            socket.Close();

        stream = null;
        receivedBuff = null;
        receivedPacket = null;
        socket = null;
    }
}