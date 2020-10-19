using System;
using System.Collections.Generic;
using System.Text;

public class MobStats
{
    public float attSpeed;
    public float movSpeed;
    public float pAttack;
    public float mAttack;
    public float pDefense;
    public float mDefense;

    public MobStats(float _attSpeed, float _movSpeed, float _pAttack, float _mAttack, float _pDefense, float _mDefense)
    {
        this.attSpeed = _attSpeed;
        this.movSpeed = _movSpeed;
        this.pAttack = _pAttack;
        this.mAttack = _mAttack;
        this.pDefense = _pDefense;
        this.mDefense = _mDefense;
    }
}
