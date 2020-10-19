using System;
using System.Collections.Generic;
using System.Text;

public class MobData
{
    public int id;
    public string name;
    public MobStats stats;

    public MobData(int _id, string _name, MobStats _stats)
    {
        this.id = _id;
        this.name = _name;
        this.stats = _stats;
    }
}