using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public enum PLAYER_RACES
{
	HUMAN = 1,
	INFECTED = 2,
	ORCS = 3
}

public enum PLAYER_SEXES
{
	MALE = 1,
	FEMALE = 1
}

public class PlayerData
{
	public int pid;
	public int aid;
	public int sid;
	public string name;
	public int level;
	public int map;
	public PLAYER_SEXES sex;
	public PLAYER_RACES race;
	public Vector3 pos;
	public int heading;
	public PlayerStats stats;
	public bool attacking;

	public PlayerData(int _pid, int _aid, int _sid, string _name, int _level, int _map, PLAYER_SEXES _sex, PLAYER_RACES _race, Vector3 _pos, int _heading, PlayerStats _stats, bool _attacking)
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
		this.attacking = _attacking;
	}
}