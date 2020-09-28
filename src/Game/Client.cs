using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Numerics;
using System.Text;

class Client
{
    private int cid;
    private static readonly int buffer_size = 524;
    private TCP _tcp;
    private int session_id;
    private Player player;

    public Client(int cid)
    {
        this.cid = cid;
        this._tcp = new TCP(this, this.cid);
        this.session_id = -1;
    }

    public Player getPlayer()
    {
        return this.player;
    }

    public int getClientId()
    {
        return this.cid;
    }

    public void setPlayer(Player player)
    {
        this.player = player;
    }

    public TCP getTcp()
    {
        return this._tcp;
    }

    public class TCP
    {
        private Client client;
        public TcpClient socket;
        private readonly int cid;
        private NetworkStream stream;
        private Packet receivedPacket;
        private byte[] receivedBuff;

        public TCP(Client _client, int _cid)
        {
            this.client = _client;
            this.cid = _cid;
        }

        public void Disconnect(int errCode = -1)
        {
            try
            {
                Logger.Syslog($"Client #{client.cid} disconnected ({client.getTcp().socket.Client.RemoteEndPoint.ToString()}) Code #{errCode.ToString()}");
            }
            catch { }

            if (socket != null)
                socket.Close();

            stream = null;
            receivedBuff = null;
            receivedPacket = null;
            socket = null;
        }

        public void Connect(TcpClient _socket)
        {
            socket = _socket;
            socket.ReceiveBufferSize = buffer_size;
            socket.SendBufferSize = buffer_size;

            stream = socket.GetStream();

            receivedPacket = new Packet();
            receivedBuff = new byte[buffer_size];

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

        public void SendData(Packet packet)
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
                Logger.Syslog($"Error sending data to the client {cid}: {ex}");
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int byteLength = stream.EndRead(ar);

                if (byteLength <= 0)
                {
                    Core.Clients[cid]._tcp.Disconnect(0);
                    return;
                }

                byte[] data = new byte[byteLength];
                Array.Copy(receivedBuff, data, byteLength);
                receivedPacket.Reset(HandleData(data));

                stream.BeginRead(receivedBuff, 0, buffer_size, ReceiveCallback, null);
            }
            catch (Exception ex)
            {
                Core.Clients[cid]._tcp.Disconnect(1);
            }
        }

        private bool HandleData(byte[] data)
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

                // Packets
                // We can handle different packets on different threads using the threadManager!
                if (Core.main_thread_packets.ContainsKey(packetId))
                {
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        Core.main_thread_packets[packetId](cid, packet);
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
    }
}
