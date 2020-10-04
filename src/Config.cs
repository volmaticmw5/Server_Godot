﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

enum ServerTypes
{
    Authentication,
    Handler,
    Game
}

struct GameServer
{
    public string label;
    public string addr;
    public int port;
    public int[] maps;
}

struct MapStruct
{
    public int id;
    public string name;
}

class ConfigStructure
{
    public ServerTypes Type;
    public int Port;
    public int MaxPlayers;
    public int MaxCharactersInAccount;
    public int Tick;
    public int MapTick;
    public string DatabaseHost;
    public int DatabasePort;
    public string DatabaseUser;
    public string DatabasePassword;
    public string DatabaseDefault;
    public int DatabaseTick;
    public int DatabasePoolSize;
    public string DatabaseAccountDb;
    public string DatabasePlayerDb;
    public string DatabaseLogDb;
    public float ViewDistance;
    public GameServer[] GameServers;
    public MapStruct[] Maps;
}

class Config
{
    public static ServerTypes Type;
    public static int Port;
    public static int MaxPlayers = 9999;
    public static int MaxCharactersInAccount;
    public static int Tick;
    public static int MapTick;
    public static string DatabaseHost;
    public static int DatabasePort;
    public static string DatabaseUser;
    public static string DatabasePassword;
    public static string DatabaseDefault;
    public static int DatabaseTick;
    public static int DatabasePoolSize;
    public static string DatabaseAccountDb;
    public static string DatabasePlayerDb;
    public static string DatabaseLogDb;
    public static float ViewDistance;
    public static GameServer[] GameServers;
    public static MapStruct[] Maps;

    public static bool ReadConfig()
    {
        if (File.Exists("config.json"))
        {
            string raw = File.ReadAllText("config.json");
            if(raw != "")
            {
                try
                {
                    ConfigStructure json = JsonConvert.DeserializeObject<ConfigStructure>(raw);
                    Type = json.Type;
                    Port = json.Port;
                    MaxPlayers = json.MaxPlayers;
                    MaxCharactersInAccount = json.MaxCharactersInAccount;
                    Tick = json.Tick;
                    try { MapTick = json.MapTick; } catch { }
                    DatabaseHost = json.DatabaseHost;
                    DatabasePort = json.DatabasePort;
                    DatabaseUser = json.DatabaseUser;
                    DatabasePassword = json.DatabasePassword;
                    DatabaseDefault = json.DatabaseDefault;
                    DatabaseTick = json.DatabaseTick;
                    DatabasePoolSize = json.DatabasePoolSize;
                    DatabaseAccountDb = json.DatabaseAccountDb;
                    DatabasePlayerDb = json.DatabasePlayerDb;
                    DatabaseLogDb = json.DatabaseLogDb;
                    ViewDistance = json.ViewDistance;
                    try
                    {
                        GameServers = json.GameServers;
                    }
                    catch { }

                    try
                    {
                        Maps = json.Maps;
                    }
                    catch { }

                    return true;
                }catch(Exception e)
                {
                    Logger.Syslog("Error reading configuration file: " + e.ToString());
                    return false;
                }
            }
            else
            {
                Logger.Syslog("Invalid configuration file!");
                return false;
            }
        }
        else
        {
            Logger.Syslog("Configuration file is missing, please make sure it exists!");
            return false;
        }
    } 
}