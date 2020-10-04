using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Numerics;
using System.Text;

class Client
{
    public int aid;
    public int cid;
    public TCP tcp;
    public int session_id { get; set; }
    public Player player;

    public Client(int cid)
    {
        this.cid = cid;
        this.tcp = new TCP(this, this.cid);
        this.session_id = -1;
    }

    public void setPlayer(Player player)
    {
        this.player = player;
    }

    public virtual void setAID(int aid)
    {
        this.aid = aid;
    }

    public virtual void setSessionId(int val)
    {
        this.session_id = val;
    }
}
