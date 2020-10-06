using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

class TCP
{
    public static readonly int buffer_size = 524;
    public Client client;
    public TcpClient socket;
    public int cid;
    public NetworkStream stream;
    public Packet receivedPacket;
    public byte[] receivedBuff;

    public TCP(Client _client, int _cid)
    {
        this.client = _client;
        this.cid = _cid;
    }

    public void configureSocket(TcpClient _socket)
    {
        socket = _socket;
        socket.ReceiveBufferSize = buffer_size;
        socket.SendBufferSize = buffer_size;
        stream = socket.GetStream();
        receivedPacket = new Packet();
        receivedBuff = new byte[buffer_size];
    }

    public void resetSocket(int byteLength)
    {
        byte[] data = new byte[byteLength];
        Array.Copy(receivedBuff, data, byteLength);
        receivedPacket.Reset(HandleData(data));
    }

    public virtual void Connect(TcpClient _socket)
    {
        configureSocket(_socket);
        stream.BeginRead(receivedBuff, 0, buffer_size, ReceiveCallback, null);

        // Ask the client for a session id 
        using (Packet newPacket = new Packet((int)Packet.ServerPackets.identifyoself))
        {
            newPacket.Write(cid);
            newPacket.Write("Welcome to the server");
            Core.SendTCPData(cid, newPacket);
        }
        try { Logger.Syslog($"Client #{client.cid} ({Core.GetClientIP(cid)}) connected to the server"); } catch { Logger.Syslog("A client connected to the server but we couldn't retrieve it's ip address."); }
    }

    public virtual void SendData(Packet packet)
    {
        try
        {
            if (socket != null)
                try { stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null); } catch { }
        }
        catch (Exception ex)
        {
            Logger.Syslog($"Error sending data to the client {cid}: {ex}");
        }
    }

    public virtual void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            int byteLength = stream.EndRead(ar);
            if (byteLength <= 0)
            {
                Server.the_core.Clients[cid].tcp.Disconnect(0);
                return;
            }

            resetSocket(byteLength);
            stream.BeginRead(receivedBuff, 0, buffer_size, ReceiveCallback, null);
        }
        catch
        {
            Server.the_core.Clients[cid].tcp.Disconnect(1);
        }
    }

    public virtual bool HandleData(byte[] data)
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

            Packet packet = new Packet(packetBytes);
            int packetId = packet.ReadInt();

            if (Core.main_thread_packets.ContainsKey(packetId))
            {
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    Core.main_thread_packets[packetId](cid, packet);
                });
            }
            else if (Core.map_thread_packets.ContainsKey(packetId))
            {
                ThreadManager.ExecuteOnMapThread(() =>
                {
                    Core.map_thread_packets[packetId](cid, packet);
                });
            }
            else
            {
                Logger.Syslog($"Received an unknown packet with id of {packetId}");
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

    public void Disconnect(int errCode = -1)
    {
        try { Logger.Syslog($"Client #{client.cid} disconnected ({client.tcp.socket.Client.RemoteEndPoint.ToString()}) Code #{errCode.ToString()}"); } catch { }

        if (client.player != null)
            client.player.Dispose();

        if (socket != null)
            socket.Close();

        stream = null;
        receivedBuff = null;
        receivedPacket = null;
        socket = null;
    }
}
