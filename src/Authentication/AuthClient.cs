using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

class AuthClient
{
    private int cid;
    private static readonly int buffer_size = 524;
    private AuthTCP _tcp;
    private int session_id;

    public AuthClient(int cid)
    {
        this.cid = cid;
        this._tcp = new AuthTCP(this, this.cid);
        this.session_id = -1;
    }

    public AuthTCP getTcp()
    {
        return this._tcp;
    }

    public int getSessionId()
    {
        return this.session_id;
    }

    public void setSessionId(int val)
    {
        this.session_id = val;
    }

    public class AuthTCP
    {
        private AuthClient client;
        public TcpClient socket;
        private readonly int cid;
        private NetworkStream stream;
        private AuthPacket receivedPacket;
        private byte[] receivedBuff;

        public AuthTCP(AuthClient _client, int _cid)
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
            }
            catch { }

            if(socket != null)
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

            receivedPacket = new AuthPacket();
            receivedBuff = new byte[buffer_size];

            stream.BeginRead(receivedBuff, 0, buffer_size, ReceiveCallback, null);

            /*
             *
             * A new client has connected to the authentication server
             * This is NOT an authentication request, its just a heartbeat 
             * If we hear back a valid PONG response then we "allow" the client to request authentication
             */
            using (Packet newPacket = new Packet((int)AuthPacket.ServerPackets.connectSucess))
            {
                newPacket.Write("Ping?");
                newPacket.Write(cid);
                AuthCore.SendTCPData(cid, newPacket);
            }
            try { Logger.Syslog($"Client #{client.cid} ({AuthCore.GetClientIP(cid)}) connected to the authentication server"); } catch { Logger.Syslog("A client connected to the authentication server but we couldn't retrieve it's ip address."); }
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
                    AuthCore.Clients[cid]._tcp.Disconnect();
                    return;
                }

                byte[] data = new byte[byteLength];
                Array.Copy(receivedBuff, data, byteLength);
                receivedPacket.packet.Reset(HandleData(data));

                stream.BeginRead(receivedBuff, 0, buffer_size, ReceiveCallback, null);
            }
            catch (Exception ex)
            {
                AuthCore.Clients[cid]._tcp.Disconnect();
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

                AuthPacket authPacket = new AuthPacket(packetBytes);
                int packetId = authPacket.packet.ReadInt();

                // Authentication packets
                // We can handle different packets on different threads using the threadManager!
                if (AuthCore.packet_handlers.ContainsKey(packetId))
                {
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        AuthCore.packet_handlers[packetId](cid, authPacket.packet);
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
