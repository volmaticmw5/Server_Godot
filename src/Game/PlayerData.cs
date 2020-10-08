using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public class PlayerData
{
	public int pid;
	public int aid;
	public int sid;
	public string name;
	public int level;
	public int map;
	public Sexes sex;
	public Races race;
	public Vector3 pos;
	public int heading;
	public PlayerStats stats;

	public PlayerData(int _pid, int _aid, int _sid, string _name, int _level, int _map, Sexes _sex, Races _race, Vector3 _pos, int _heading, PlayerStats _stats)
	{
		this.pid = _pid;
		this.aid = _aid;
		this.sid = _sid;
		this.name = _name;
		this.level = _level;
		this.map = _map;
		this.sex = _sex;
		this.race = _race;
		this.pos = _pos;
		this.stats = _stats;
		this.heading = _heading;
	}
}