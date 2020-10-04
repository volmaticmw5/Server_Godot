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
    public int session { get; set; }
    public string name { get; set; }
    public int pid { get; set; }
    public int aid { get; set; }
    public int map { get; set; }
    public Sexes sex { get; set; }
    public Races race { get; set; }
    public Vector3 pos { get; set; }
    public Client client { get; set; }
    public PlayerStats stats { get; set; }

    public Player(Client _client, int _session, int _pid, int _aid, Sexes _sex, Races _race, PlayerStats _stats)
    {
        this.client = _client;
        this.session = _session;
        this.pid = _pid;
        this.aid = _aid;
        this.sex = _sex;
        this.race = _race;
        this.stats = _stats;
    }
    ~Player() { }

    public async void Dispose()
    {
        Logger.Syslog($"Player with session id {session} dumped and destroyed.");

        // Dump player data to the database
        string statsRaw = JsonConvert.SerializeObject(this.stats);
        List<MySqlParameter> dumpParams = new List<MySqlParameter>()
        {
            MySQL_Param.Parameter("?pid", pid),
            MySQL_Param.Parameter("?x", this.pos.X.ToString("0.000")), // round to .000f
            MySQL_Param.Parameter("?y", this.pos.Y.ToString("0.000")), // round to .000f
            MySQL_Param.Parameter("?z", this.pos.Z.ToString("0.000")), // round to .000f
            MySQL_Param.Parameter("?map", this.map),
            MySQL_Param.Parameter("?stats", statsRaw),
        };
        await Server.DB.QueryAsync("UPDATE [[player]].player SET `x`=?x, `y`=?y, `z`=?z, `map`=?map, `stats`=?stats WHERE `id`=?pid LIMIT 1", dumpParams);

        List<MySqlParameter> _params = new List<MySqlParameter>()
        {
            MySQL_Param.Parameter("?session", session),
            MySQL_Param.Parameter("?aid", aid),
            MySQL_Param.Parameter("?pid", pid),
        };
        await Server.DB.QueryAsync("DELETE FROM [[player]].sessions WHERE `session`=?session AND `pid`=?pid AND `aid`=?aid LIMIT 1", _params);
    }
}
