using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public class Mob
{
    public static readonly int MOB_ATTACK_SPEED_MODIFIER = 20;
    public static readonly float MOB_WALK_SPEED_MODIFIER = 0.005f;

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

    public void Update()
    {
        if (currentHp <= 0)
            return;

        updateFocus();
        if (validFocus())
            chaseOrAttack();
        else
            wander();
    }

    private int generateMobId()
    {
        Random rnd = new Random();
        return Math.Abs(this.data.id + (int)MathHelp.TimestampMiliseconds() + owner.id + rnd.Next(1,Int32.MaxValue));
    }

    private bool validFocus()
    {
        if(gid != 0)
        {
            Mob[] mobs = owner.getMobsInGroup(gid);
            for (int i = 0; i < mobs.Length; i++)
            {
                if (mobs[i].focus != null)
                {
                    focus = mobs[i].focus;
                    return true;
                }
            }
        }

        if (focus == null)
            return false;
        if (!focus.isAlive())
            return false;
        if (Server.the_core.Clients[focus.client.cid].player == null)
            return false;
        if (Vector3.Distance(position, focus.data.pos) > 30f)
            return false;

        foreach (KeyValuePair<Player,float> player in hittenBy)
        {
            if(player.Key != null)
            {
                if (!player.Key.isAlive())
                    continue;
                if (Server.the_core.Clients[player.Key.client.cid].player == null)
                    continue;

                focus = player.Key;
                return true;
            }
        }

        return true;
    }

    private void getNewFocus()
    {
        float lastVal = 0f;
        Player newFocus = null;
        foreach (KeyValuePair<Player, float> entry in hittenBy)
        {
            if (entry.Value > lastVal)
                newFocus = entry.Key;
            lastVal = entry.Value;
        }

        focus = newFocus;
        if (!validFocus())
        {
            hittenBy.Remove(newFocus);
            focus = null;
        }
    }

    private void updateFocus()
    {
        foreach (KeyValuePair<Player, float> player in hittenBy)
        {
            if (player.Key != null && Server.the_core.Clients[player.Key.client.cid].player != null)
            {
                focus = player.Key;
                if (gid != 0)
                    broadcastFocus();
                return;
            }
        }
    }

    private void broadcastFocus()
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

    private void wander()
    {
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
            if (wanderDelta == 0f)
            {
                Random rnd = new Random();
                float wanderX = Math.Clamp(startingWalkPoint.X + (rnd.Next(1, 500) / 100f), 0, owner.width);
                float wanderZ = Math.Clamp(startingWalkPoint.Z + (rnd.Next(1, 500) / 100f), 0, owner.height);
                float wanderY = owner.heightMap[(int)wanderX, (int)wanderZ];
                wanderPos = new Vector3(wanderX, wanderY, wanderZ);
            }

            wanderDelta += MOB_WALK_SPEED_MODIFIER * (data.stats.movSpeed * Math.Abs(Vector3.Distance(position, wanderPos)));
            wanderDelta = Math.Clamp(wanderDelta, 0f, 1f);
            if (wanderDelta == 1f)
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

    private void chaseOrAttack()
    {
        if (Vector3.Distance(position, focus.data.pos) <= (0.001f + data.stats.attRange))
        {
            chaseDelta = 0f;

            if (toWaitbeforeAttack == 0)
            {
                float damage = 1f + (data.stats.pAttack - focus.stats.pDefense) + (data.stats.mAttack - focus.stats.mDefense);
                toWaitbeforeAttack = (int)data.stats.attSpeed * MOB_ATTACK_SPEED_MODIFIER;
                focus.receiveDamage(damage);
            }
            else
                toWaitbeforeAttack--;

            return;
        }
        else
        {
            chaseDelta += MOB_WALK_SPEED_MODIFIER * (data.stats.movSpeed * Math.Abs(Vector3.Distance(position, focus.data.pos)));
            chaseDelta = Math.Clamp(chaseDelta, 0f, 1f);
            position = MathHelp.Lerp(position, focus.data.pos, chaseDelta);
            toWaitbeforeAttack = (int)data.stats.attSpeed * MOB_ATTACK_SPEED_MODIFIER;
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
                mobs[i].chaseDelta = 0f;
                mobs[i].wanderKey = 0;
            }
        }

        focus = null;
        hittenBy.Clear();
        chaseDelta = 0f;
        wanderKey = 0;
    }

    public void receiveDamage(Player fromPlayer, float damage)
    {
        if (!hittenBy.ContainsKey(fromPlayer))
            hittenBy.Add(fromPlayer, damage);
        else
            hittenBy[fromPlayer] += damage;

        getNewFocus();
        if (gid != 0 && validFocus())
            broadcastFocus();

        this.currentHp -= damage;
        if (this.currentHp <= 0f)
            Die();
    }

    public void Die()
    {
        owner.removeFromMobList(mid);
        focus = null;
        owner = null;
    }
}