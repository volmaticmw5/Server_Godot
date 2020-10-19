using System;
using System.Collections.Generic;
using System.Text;

public enum ITEM_TYPES
{
    NONE,
    WEAPON,
    USE_ITEM
}

public enum ITEM_SUB_TYPES
{
    NONE,
    SWORD,
    MACE,
    AXE,
    FAN,
    STAFF,
    BOW,
}

public enum BONUS_TYPE
{
    NONE,
    P_ATTACK,
    M_ATTACK,
    P_DEF,
    M_DEF,
    ATT_SPEED,
    HEALTH_REG,
    MANA_REG,
    MOVE_SPEED
}

public class ItemData
{
    public int vnum;
    public string name;
    public int level;
    public PLAYER_RACES[] races;
    public int size;
    public bool stacks;
    public ITEM_TYPES type;
    public ITEM_SUB_TYPES sub_type;
    public BONUS_TYPE bonus_type0;
    public float bonus_value0;
    public BONUS_TYPE bonus_type1;
    public float bonus_value1;
    public BONUS_TYPE bonus_type2;
    public float bonus_value2;
    public BONUS_TYPE bonus_type3;
    public float bonus_value3;
    public BONUS_TYPE bonus_type4;
    public float bonus_value4;
    public BONUS_TYPE bonus_type5;
    public float bonus_value5;

    public ItemData(int id, string name, int level, PLAYER_RACES[] races, int size, bool _stacks, ITEM_TYPES type, ITEM_SUB_TYPES sub_type, BONUS_TYPE bonus_type0, float bonus_value0, BONUS_TYPE bonus_type1, float bonus_value1, BONUS_TYPE bonus_type2, float bonus_value2, BONUS_TYPE bonus_type3, float bonus_value3, BONUS_TYPE bonus_type4, float bonus_value4, BONUS_TYPE bonus_type5, float bonus_value5)
    {
        this.vnum = id;
        this.name = name;
        this.level = level;
        this.races = races;
        this.size = size;
        this.stacks = _stacks;
        this.type = type;
        this.sub_type = sub_type;
        this.bonus_type0 = bonus_type0;
        this.bonus_value0 = bonus_value0;
        this.bonus_type1 = bonus_type1;
        this.bonus_value1 = bonus_value1;
        this.bonus_type2 = bonus_type2;
        this.bonus_value2 = bonus_value2;
        this.bonus_type3 = bonus_type3;
        this.bonus_value3 = bonus_value3;
        this.bonus_type4 = bonus_type4;
        this.bonus_value4 = bonus_value4;
        this.bonus_type5 = bonus_type5;
        this.bonus_value5 = bonus_value5;
    }
}
