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
	public int exp;
	public int vit, str, _int, dex;
	public int map;
	public PLAYER_SEXES sex;
	public PLAYER_RACES race;
	public Vector3 pos;
	public int heading;
	public PlayerStats stats;
	public bool attacking;
	public float maxHp;
	public float maxMana;
	public float hp;
	public float mana;

	public PlayerData(int _pid, string _name, int _level, int _map, PLAYER_SEXES _sex, PLAYER_RACES _race, Vector3 _pos, int _heading, PlayerStats _stats, bool _attacking, int _aid = 0, int _sid = 0, float _maxHp = 10, float _hp = 10, float _mn = 10, float _maxMn = 10, int _exp = 0, int _vit = 0, int _str = 0, int _int = 0, int _dex = 0)
	{
		this.pid = _pid;
		this.aid = _aid;
		this.sid = _sid;
		this.name = _name;
		this.level = _level;
		this.exp = _exp;
		this.vit = _vit;
		this.str = _str;
		this._int = _int;
		this.dex = _dex;
		this.map = _map;
		this.sex = _sex;
		this.race = _race;
		this.pos = _pos;
		this.stats = _stats;
		this.heading = _heading;
		this.attacking = _attacking;
		this.maxHp = _maxHp;
		this.maxMana = _maxMn;
		this.hp = _hp;
		this.mana = _mn;
	}
}