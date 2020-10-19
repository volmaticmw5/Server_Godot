using System;
using System.Collections.Generic;
using System.Text;

class Mob
{
    public int mid;
    public int gid;
    public MobData data { get; private set; }

    public Mob(MobData _data)
    {
        this.data = data;
    }
}