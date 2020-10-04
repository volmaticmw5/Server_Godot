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

    public static int TimestampSeconds()
    {
        return (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
    }

    public static long TimestampMiliseconds()
    {
        return DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }
}
