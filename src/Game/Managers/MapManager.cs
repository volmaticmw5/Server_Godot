using System;
using System.Collections.Generic;
using System.Text;

class MapManager
{
    private static List<Map> Maps = new List<Map>();

    public static void AddConfigMapsToMapManager()
    {
        foreach (MapStruct map in Config.Maps)
        {
            Map nMap = new Map(map.id, map.name);
            MapManager.AddMapToManager(nMap);
        }
    }

    public static void Update()
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