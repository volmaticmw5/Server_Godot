using System;
using System.Collections.Generic;
using System.Text;

public enum MOB_WALK_TYPE
{
    STILL,
    WANDER
}

public class MobStats
{
    public MOB_WALK_TYPE walkType;
    public float wanderRadius;
    public int wanderWaitTime;
    public float maxHp;
    public float hpRegen;
    public float attSpeed;
    public float movSpeed;
    public float pAttack;
    public float mAttack;
    public float pDefense;
    public float mDefense;
    public float attRange;

    public MobStats(MOB_WALK_TYPE _walk_type, float wander_radius, int wanderWait, float _maxHp, float _hpRegen, float _attSpeed, float _movSpeed, float _pAttack, float _mAttack, float _pDefense, float _mDefense, float _attRange)
    {
        this.walkType = _walk_type;
        this.wanderRadius = wander_radius;
        this.wanderWaitTime = wanderWait;
        this.maxHp = _maxHp;
        this.hpRegen = _hpRegen;
        this.attSpeed = _attSpeed;
        this.movSpeed = _movSpeed;
        this.pAttack = _pAttack;
        this.mAttack = _mAttack;
        this.pDefense = _pDefense;
        this.mDefense = _mDefense;
        this.attRange = _attRange;
    }
}
