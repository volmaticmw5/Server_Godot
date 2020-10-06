using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

class ChatClient : Client
{
    public new ChatTCP tcp;

    public ChatClient(int cid) : base(cid)
    {
        this.cid = cid;
        this.tcp = new ChatTCP(this, this.cid);
        this.session_id = -1;
    }
}
