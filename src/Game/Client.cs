using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

class Client
{
    private int cid;
    private static readonly int buffer_size = 524;
    private TCP _tcp;
    private int aid;
    private int session_id;

    public Client(int cid)
    {
        this.cid = cid;
        this._tcp = new TCP(this, this.cid);
        this.session_id = -1;
    }

    public TCP getTcp()
    {
        return this._tcp;
    }

    public int getAID()
    {
        return this.aid;
    }

    public void setAID(int val)
    {
        this.aid = val;
    }

    public int getSessionId()
    {
        return this.session_id;
    }

    public void setSessionId(int val)
    {
        this.session_id = val;
    }

    public class TCP
    {
        private Client client;
        public TcpClient socket;
        private readonly int cid;
        private NetworkStream stream;
        private CorePacket receivedPacket;
        private byte[] receivedBuff;

        public TCP(Client _client, int _cid)
        {
            this.client = _client;
            this.cid = _cid;
        }

        public void Disconnect()
        {
            try
            {
                Logger.Syslog($"Client #{client.cid} disconnected ({client.getTcp().socket.Client.RemoteEndPoint.ToString()})");
                client.setSessionId(-1);
                client.setAID(-1);
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

            receivedPacket = new CorePacket();
            receivedBuff = new byte[buffer_size];

            stream.BeginRead(receivedBuff, 0, buffer_size, ReceiveCallback, null);

            // Ask the client for a session id 
            using (Packet newPacket = new Packet((int)CorePacket.ServerPackets.identifyoself))
            {
                newPacket.Write(cid);
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
                    Core.Clients[cid]._tcp.Disconnect();
                    return;
                }

                byte[] data = new byte[byteLength];
                Array.Copy(receivedBuff, data, byteLength);
                receivedPacket.packet.Reset(HandleData(data));

                stream.BeginRead(receivedBuff, 0, buffer_size, ReceiveCallback, null);
            }
            catch (Exception ex)
            {
                Core.Clients[cid]._tcp.Disconnect();
            }
        }

        private bool HandleData(byte[] data)
        {
            int packetLength = 0;
            receivedPacket.packet.SetBytes(data);
            if (receivedPacket.packet.UnreadLength() >= 4)
            {
                packetLength = receivedPacket.packet.ReadInt();
                if (packetLength <= 0)
                    return true;
            }

            while (packetLength > 0 && packetLength <= receivedPacket.packet.UnreadLength())
            {
                byte[] packetBytes = receivedPacket.packet.ReadBytes(packetLength);

                CorePacket packet = new CorePacket(packetBytes);
                int packetId = packet.packet.ReadInt();

                // Packets
                // We can handle different packets on different threads using the threadManager!
                if (Core.main_thread_packets.ContainsKey(packetId))
                {
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        Core.main_thread_packets[packetId](cid, packet.packet);
                    });
                }
                else
                {
                    Logger.Syslog($"Received an unknown packet with id of {packetId}");
                }


                packetLength = 0;
                if (receivedPacket.packet.UnreadLength() >= 4)
                {
                    packetLength = receivedPacket.packet.ReadInt();
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
