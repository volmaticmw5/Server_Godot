using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public class Mob
{
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

        WalkOrFocus();
    }

    private void WalkOrFocus()
    {
        if (data.stats.walkType == MOB_WALK_TYPE.STILL)
            return;

        if (focus != null)
        {

            msLeftForWalkUpdate = data.stats.wanderWaitTime;
        }
        else
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
    }

    private void doWanderCycle()
    {
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

            wanderDelta += data.stats.movSpeed / Math.Abs(Vector3.Distance(position, wanderPos));
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

    public void receiveDamage(float damage)
    {
        Logger.Syslog($"Receive {damage} dmg, current hp: {currentHp}");
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