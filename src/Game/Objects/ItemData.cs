﻿using System;
using System.Collections.Generic;
using System.Text;

public enum ITEM_TYPES
{
    NONE,
    WEAPON,
    ARMOR,
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

    // SPECIFIC FOR WEAPONS, MAY NOT BE IN USE
    public float pDamage;
    public float mDamage;

    public ItemData(int id, string name, int level, PLAYER_RACES[] races, bool _stacks, ITEM_TYPES type, ITEM_SUB_TYPES sub_type, BONUS_TYPE bonus_type0, float bonus_value0, BONUS_TYPE bonus_type1, float bonus_value1, BONUS_TYPE bonus_type2, float bonus_value2, BONUS_TYPE bonus_type3, float bonus_value3, BONUS_TYPE bonus_type4, float bonus_value4, BONUS_TYPE bonus_type5, float bonus_value5)
    {
        this.vnum = id;
        this.name = name;
        this.level = level;
        this.races = races;
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

        if(type == ITEM_TYPES.WEAPON)
        {
            if (bonus_type0 == BONUS_TYPE.P_ATTACK)
                this.pDamage = bonus_value0;
            if (bonus_type1 == BONUS_TYPE.P_ATTACK)
                this.pDamage = bonus_value1;
            if (bonus_type2 == BONUS_TYPE.P_ATTACK)
                this.pDamage = bonus_value2;
            if (bonus_type3 == BONUS_TYPE.P_ATTACK)
                this.pDamage = bonus_value3;
            if (bonus_type4 == BONUS_TYPE.P_ATTACK)
                this.pDamage = bonus_value4;
            if (bonus_type5 == BONUS_TYPE.P_ATTACK)
                this.pDamage = bonus_value5;

            if (bonus_type0 == BONUS_TYPE.M_ATTACK)
                this.mDamage = bonus_value0;
            if (bonus_type1 == BONUS_TYPE.M_ATTACK)
                this.mDamage = bonus_value1;
            if (bonus_type2 == BONUS_TYPE.M_ATTACK)
                this.mDamage = bonus_value2;
            if (bonus_type3 == BONUS_TYPE.M_ATTACK)
                this.mDamage = bonus_value3;
            if (bonus_type4 == BONUS_TYPE.M_ATTACK)
                this.mDamage = bonus_value4;
            if (bonus_type5 == BONUS_TYPE.M_ATTACK)
                this.mDamage = bonus_value5;
        }
    }
}
