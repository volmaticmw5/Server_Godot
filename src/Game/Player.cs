using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

class Player
{
    private int session;
    private string name;
    private int pid;
    private int aid;
    private int map;
    private Vector3 pos;
    private Client client;

    public Player(Client _client, int _session, int _pid, int _aid)
    {
        this.client = _client;
        this.session = _session;
        this.pid = _pid;
        this.aid = _aid;
    }

    ~Player() 
    {
        Logger.Syslog($"Player with session id {session} destroyed.");
        List<MySqlParameter> _params = new List<MySqlParameter>()
            {
                MySQL_Param.Parameter("?session", session),
                MySQL_Param.Parameter("?aid", aid),
                MySQL_Param.Parameter("?pid", pid),
            };
        Server.DB.AddQueryToQeue("DELETE FROM [[player]].sessions WHERE `session`=?session AND `pid`=?pid AND `aid`=?aid LIMIT 1", _params, client.getClientId());
    }

    public int getSession()
    {
        return this.session;
    }

    public int getPid()
    {
        return this.pid;
    }

    public int getAid()
    {
        return this.aid;
    }

    public int getMap()
    {
        return this.map;
    }

    public Vector3 getPos()
    {
        return this.pos;
    }

    public void setMap(int _map)
    {
        this.map = _map;
    }

    public void setPos(Vector3 _pos)
    {
        this.pos = _pos;
    }

    public void setName(string _name)
    {
        this.name = _name;
    }

    public string getName()
    {
        return this.name;
    }
}
