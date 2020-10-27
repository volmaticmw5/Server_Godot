using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public class NpcSpawnData
{
    public enum NPC_SPAWN_TYPE
    {
        GROUP,
        MOB
    }

    public NPC_SPAWN_TYPE type;
    public int id;
    public Vector2 pos;
    public int time;

    public NpcSpawnData(NPC_SPAWN_TYPE _type, int _id, Vector2 _pos, int _time)
    {
        this.type = _type;
        this.id = _id;
        this.pos = _pos;
        this.time = _time;
    }
}
