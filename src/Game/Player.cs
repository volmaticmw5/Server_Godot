using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

class Player
{
    public enum Races
    {
        HUMAN = 1,
        INFECTED = 2,
        ORCS = 3
    }

    public enum Sexes
    {
        MALE = 1,
        FEMALE = 1
    }

    public int session { get; set; }
    public string name { get; set; }
    public int pid { get; set; }
    public int aid { get; set; }
    public int map { get; set; }
    public Sexes sex { get; set; }
    public Races race { get; set; }
    public Vector3 pos { get; set; }
    public Client client { get; set; }

    public Player(Client _client, int _session, int _pid, int _aid, Sexes _sex, Races _race)
    {
        this.client = _client;
        this.session = _session;
        this.pid = _pid;
        this.aid = _aid;
        this.sex = _sex;
        this.race = _race;
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
}
