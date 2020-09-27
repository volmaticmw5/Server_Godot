using System;
using System.Collections.Generic;
using System.Text;

class MapManager
{
    private static List<Map> Maps = new List<Map>();

    public static void Tick()
    {
        foreach (Map map in Maps)
        {
            map.Update();
        }
    }

    public static void AddMapToManager(Map map)
    {
        Maps.Add(map);
    }
}