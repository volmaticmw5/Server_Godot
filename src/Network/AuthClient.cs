using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

class AuthClient : Client
{
    public new AuthTCP tcp;

    public AuthClient(int cid) : base(cid)
    {
        this.cid = cid;
        this.tcp = new AuthTCP(this, this.cid);
        this.session_id = -1;
    }
}
