using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

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

class Player
{
    public int session;
    public string name;
    public int pid;
    public int aid;
    public int map;
    public int level;
    public Sexes sex;
    public Races race;
    public Vector3 pos;
    public int heading;
    public Client client;
    public PlayerStats stats;

    public Player(Client _client, int _session, int _pid, int _aid, int _level, Sexes _sex, Races _race, Vector3 _pos, int _heading, PlayerStats _stats)
    {
        this.client = _client;
        this.session = _session;
        this.pid = _pid;
        this.aid = _aid;
        this.sex = _sex;
        this.race = _race;
        this.stats = _stats;
        this.level = _level;
        this.pos = _pos;
        this.heading = _heading;
    }
    ~Player() { }

    public async void Dispose()
    {
        string statsRaw = JsonConvert.SerializeObject(this.stats);
        List<MySqlParameter> dumpParams = new List<MySqlParameter>()
        {
            MySQL_Param.Parameter("?pid", pid),
            MySQL_Param.Parameter("?level", level),
            MySQL_Param.Parameter("?x", this.pos.X.ToString("0.000")),
            MySQL_Param.Parameter("?y", this.pos.Y.ToString("0.000")),
            MySQL_Param.Parameter("?z", this.pos.Z.ToString("0.000")),
            MySQL_Param.Parameter("?h", this.heading),
            MySQL_Param.Parameter("?map", this.map),
            MySQL_Param.Parameter("?stats", statsRaw),
        };
        await Server.DB.QueryAsync("UPDATE [[player]].player SET `level`=?level, `x`=?x, `y`=?y, `z`=?z, `h`=?h, `map`=?map, `stats`=?stats WHERE `id`=?pid LIMIT 1", dumpParams);

        List<MySqlParameter> _params = new List<MySqlParameter>()
        {
            MySQL_Param.Parameter("?session", session),
            MySQL_Param.Parameter("?aid", aid),
            MySQL_Param.Parameter("?pid", pid),
        };
        await Server.DB.QueryAsync("DELETE FROM [[player]].sessions WHERE `session`=?session AND `pid`=?pid AND `aid`=?aid LIMIT 1", _params);
        Logger.Syslog($"Player with session id {session} dumped and destroyed.");
    }

    public void UpdatePosition(Vector3 newPos, int newHeading)
    {
        this.pos = newPos;
        this.heading = newHeading;
    }
}
