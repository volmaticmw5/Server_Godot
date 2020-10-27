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
            Map nMap = new Map(map.id, map.name, map.width, map.height);
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

    public static Map getMapById(int id)
    {
        for (int i = 0; i < Maps.Count; i++)
        {
            if (Maps[i].id == id)
                return Maps[i];
        }
        return null;
    }
}