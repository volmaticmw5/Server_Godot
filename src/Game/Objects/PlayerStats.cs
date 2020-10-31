using System;
using System.Collections.Generic;
using System.Text;

public class PlayerStats
{
    public float maxHp;
    public float maxMana;
    public float movementSpeed;
    public float attackSpeed;
    public float pAttack;
    public float mAttack;
    public float pDefense;
    public float mDefense;

    public PlayerStats(float _movementSpeed = 1f, float _attackSpeed = 1f, float _pAttack = 1f, float _mAttack = 1f, float _maxHp = 100f, float _maxMana = 100f, float _pDef = 1f, float _mDef = 1f)
    {
        this.movementSpeed = _movementSpeed;
        this.attackSpeed = _attackSpeed;
        this.pAttack = _pAttack;
        this.mAttack = _mAttack;
        this.maxHp = _maxHp;
        this.maxMana = _maxMana;
        this.pDefense = _pDef;
        this.mDefense = _mDef;
    }
}
