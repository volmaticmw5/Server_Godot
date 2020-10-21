using System;
using System.Collections.Generic;
using System.Text;

public class PlayerStats
{
    public float movementSpeed;
    public float attackSpeed;
    public float pAttack;
    public float mAttack;

    public PlayerStats(float _movementSpeed = 1f, float _attackSpeed = 1f, float _pAttack = 1f, float _mAttack = 1f)
    {
        this.movementSpeed = _movementSpeed;
        this.attackSpeed = _attackSpeed;
        this.pAttack = _pAttack;
        this.mAttack = _mAttack;
    }
}
