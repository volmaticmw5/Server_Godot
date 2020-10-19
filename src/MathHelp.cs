using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

class MathHelp
{
    public static float Lerp(float from, float to, float time)
    {
        return (from + time * (to - from));
    }

    public static Vector2 Lerp(Vector2 from, Vector2 to, float by)
    {
        float retX = Lerp(from.X, to.X, by);
        float retY = Lerp(from.Y, to.Y, by);
        return new Vector2(retX, retY);
    }

    public static Vector3 Lerp(Vector3 from, Vector3 to, float by)
    {
        float retX = Lerp(from.X, to.X, by);
        float retY = Lerp(from.Y, to.Y, by);
        float retZ = Lerp(from.Z, to.Z, by);
        return new Vector3(retX, retY, retZ);
    }

    public static long LongRandom(long min, long max, Random rand)
    {
        byte[] buf = new byte[8];
        rand.NextBytes(buf);
        long longRand = BitConverter.ToInt64(buf, 0);

        return (Math.Abs(longRand % (max - min)) + min);
    }

    public static int TimestampSeconds()
    {
        return (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
    }

    public static long TimestampMiliseconds()
    {
        return DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }
}
