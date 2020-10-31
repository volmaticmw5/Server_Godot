using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public class Mob
{
    public static readonly int MOB_ATTACK_SPEED_MODIFIER = 20;

    public int mid;
    public int gid;
    public MobData data { get; private set; }
    public Map owner;
    public Player focus;

    public float currentHp;
    public Vector3 position;
    public float respawnTime;
    private int msLeftForWalkUpdate = 0;
    private int wanderKey = 0;
    private Vector3 startingWalkPoint = Vector3.Zero;
    private Vector3 wanderPos = Vector3.Zero;
    private float wanderDelta = 0f;
    private float chaseDelta = 0f;
    private Dictionary<Player, float> hittenBy = new Dictionary<Player, float>();
    private bool inAttackRange;
    private int toWaitbeforeAttack;

    public Mob(MobData _data, Map _owner, Vector3 _pos, float _respawn_time, int _gid = 0, bool relativePos = true)
    {
        Random rnd = new Random();
        this.data = _data;
        this.owner = _owner;
        this.mid = generateMobId();
        this.gid = _gid;
        if(relativePos)
        {
            float newX = Math.Clamp(_pos.X + rnd.Next(1, 4), 0, owner.width);
            float newZ = Math.Clamp(_pos.Z + rnd.Next(1, 4), 0, owner.height);
            this.position = new Vector3(newX, owner.heightMap[(int)newX, (int)newZ], newZ);
        }
        else
        {
            this.position = _pos;
        }
        this.respawnTime = _respawn_time;

        this.currentHp = data.stats.maxHp;
    }

    private int generateMobId()
    {
        Random rnd = new Random();
        return Math.Abs(this.data.id + (int)MathHelp.TimestampMiliseconds() + owner.id + rnd.Next(1,Int32.MaxValue));
    }

    public void Update()
    {
        if (currentHp <= 0)
            return;

        if(gid != 0)
        {
            Mob[] mobs = owner.getMobsInGroup(gid);
            for (int i = 0; i < mobs.Length; i++)
            {
                if (mobs[i].focus != null)
                {
                    focus = mobs[i].focus;
                    break;
                }
            }
        }

        if(focus != null)
        {
            if (inAttackRange)
                attackFocus();
            else
                toWaitbeforeAttack = (int)data.stats.attSpeed * MOB_ATTACK_SPEED_MODIFIER;
        }

        walkOrFocus();
    }

    private void attackFocus()
    {
        if (focus == null)
        {
            focus = getNewFocus();
            if (focus == null)
                ClearFocus();
            if (!focus.isAlive())
                ClearFocus();
        }

        if (!inAttackRange)
            return;
        if (focus == null)
            return;

        if (toWaitbeforeAttack == 0)
        {
            float damage = 1f + (data.stats.pAttack - focus.stats.pDefense) + (data.stats.mAttack - focus.stats.mDefense);
            toWaitbeforeAttack = (int)data.stats.attSpeed * MOB_ATTACK_SPEED_MODIFIER;
            focus.receiveDamage(damage);
        }
        if (toWaitbeforeAttack > 0)
        {
            toWaitbeforeAttack--;
        }
    }

    private void walkOrFocus()
    {
        if (data.stats.walkType == MOB_WALK_TYPE.STILL)
            return;

        if (focus != null)
        {
            chaseFocus();
            //msLeftForWalkUpdate = data.stats.wanderWaitTime;
        }
        else
        {
            if (gid != 0)
            {
                Mob[] mobs = owner.getMobsInGroup(gid);
                for (int i = 0; i < mobs.Length; i++)
                {
                    if (mobs[i].focus != null)
                    {
                        focus = mobs[i].focus;
                        break;
                    }
                }
                if(focus != null)
                    chaseFocus();
            }
            
            if (msLeftForWalkUpdate <= 0)
            {
            doWanderCycle();
            msLeftForWalkUpdate = data.stats.wanderWaitTime;
            }
            else
            {
            msLeftForWalkUpdate -= Config.MapTick;
            }
        }
    }

    public void ClearFocus()
    {
        if(gid != 0)
        {
            Mob[] mobs = owner.getMobsInGroup(gid);
            for (int i = 0; i < mobs.Length; i++)
            {
                mobs[i].focus = null;
                mobs[i].hittenBy.Clear();
                mobs[i].inAttackRange = false;
                mobs[i].chaseDelta = 0f;
                mobs[i].wanderKey = 0;
            }
        }

        focus = null;
        hittenBy.Clear();
        inAttackRange = false;
        chaseDelta = 0f;
        wanderKey = 0;
    }

    private void chaseFocus()
    {
        if (!focus.isAlive())
        {
            if (hittenBy.ContainsKey(focus))
                hittenBy.Remove(focus);
            focus = getNewFocus();
        }

        if (focus == null)
        {
            ClearFocus();
            return;
        }

        if(Vector3.Distance(position, focus.pos) <= (0.001f + data.stats.attRange))
        {
            chaseDelta = 0f;
            inAttackRange = true;
            return;
        }
        else
        {
            inAttackRange = false;
            chaseDelta += 0.005f * (data.stats.movSpeed * Math.Abs(Vector3.Distance(position, focus.pos)));
            chaseDelta = Math.Clamp(chaseDelta, 0f, 1f);
            position = MathHelp.Lerp(position, focus.pos, chaseDelta);
        }
    }

    private void doWanderCycle()
    {
        if (hittenBy.Count != 0)
            hittenBy.Clear();

        if (data.stats.wanderRadius == 0f)
            return;

        if (startingWalkPoint == Vector3.Zero)
            startingWalkPoint = position;
  
        if (wanderKey == 0)
        {
            if(wanderDelta == 0f)
            {
                Random rnd = new Random();
                float wanderX = Math.Clamp(startingWalkPoint.X + (rnd.Next(1, 500) / 100f), 0, owner.width);
                float wanderZ = Math.Clamp(startingWalkPoint.Z + (rnd.Next(1, 500) / 100f), 0, owner.height);
                float wanderY = owner.heightMap[(int)wanderX, (int)wanderZ];
                wanderPos = new Vector3(wanderX, wanderY, wanderZ);
            }

            wanderDelta += 0.005f * (data.stats.movSpeed * Math.Abs(Vector3.Distance(position, wanderPos)));
            wanderDelta = Math.Clamp(wanderDelta, 0f, 1f);
            if(wanderDelta == 1f)
            {
                position = wanderPos;
                wanderKey = 1;
                wanderDelta = 0f;
            }
            else
            {
                position = MathHelp.Lerp(position, wanderPos, wanderDelta);
            }
        }
        else
        {
            wanderDelta += data.stats.movSpeed / Math.Abs(Vector3.Distance(position, startingWalkPoint));
            wanderDelta = Math.Clamp(wanderDelta, 0f, 1f);
            if (wanderDelta == 1f)
            {
                position = startingWalkPoint;
                wanderKey = 0;
                wanderDelta = 0f;
            }
            else
            {
                position = MathHelp.Lerp(position, startingWalkPoint, wanderDelta);
            }
        }
    }

    public void receiveDamage(Player fromPlayer, float damage)
    {
        if (!hittenBy.ContainsKey(fromPlayer))
            hittenBy.Add(fromPlayer, damage);
        else
            hittenBy[fromPlayer] += damage;

        focus = getNewFocus();
        if(gid != 0)
        {
            Mob[] mobs = owner.getMobsInGroup(gid);
            Logger.Syslog($"Alerting {mobs.Length} mobs in this group");
            for (int i = 0; i < mobs.Length; i++)
                mobs[i].focus = focus;
        }

        this.currentHp -= damage;
        if (this.currentHp <= 0f)
            Die();
    }

    private Player getNewFocus()
    {
        float lastVal = 0f;
        Player newFocus = null;
        foreach (KeyValuePair<Player,float> entry in hittenBy)
        {
            if (entry.Value > lastVal)
                newFocus = entry.Key;
        }

        if (newFocus == null)
            return null;

        if (Vector3.Distance(newFocus.pos, this.position) > 30f)
        {
            hittenBy.Remove(newFocus);
            newFocus = null;
        }

        return newFocus;
    }

    public void Die()
    {
        owner.removeFromMobList(mid);
        focus = null;
        owner = null;
    }
}