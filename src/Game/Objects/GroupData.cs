using System;
using System.Collections.Generic;
using System.Text;

public class GroupData
{
    public int id;
    public string label;
    public int[] mobIds;

    public GroupData(int _id, string _label, int[] _mobs)
    {
        this.id = _id;
        this.label = _label;
        this.mobIds = _mobs;
    }
}