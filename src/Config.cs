using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

enum ServerTypes
{
    Authentication,
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
    public int width;
    public int height;
}

class ConfigStructure
{
    public ServerTypes Type;
    public bool WriteToConsole;
    public bool DbLogsEnabled;
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
    public string LocalePath;
    public string QuestsPath;
    public int MaxStackItems;
    public long EpochStart;
    public GameServer[] GameServers;
    public MapStruct[] Maps;
}

class Config
{
    public static ServerTypes Type;
    public static bool WriteToConsole;
    public static bool DbLogsEnabled;
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
    public static string LocalePath;
    public static string QuestsPath;
    public static int MaxStackItems;
    public static long EpochStart;
    public static GameServer[] GameServers;
    public static MapStruct[] Maps;

    // Not in the config structure
    public static MobData[] Mobs;
    public static GroupData[] MobGroups;
    public static ItemData[] Items;

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
                    WriteToConsole = json.WriteToConsole;
                    DbLogsEnabled = json.DbLogsEnabled;
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
                    LocalePath = json.LocalePath;
                    QuestsPath = json.QuestsPath;
                    MaxStackItems = json.MaxStackItems;
                    EpochStart = json.EpochStart;
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
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    public static bool ReadGroupData()
    {
        if (!File.Exists(Config.LocalePath + "group_data"))
            return false;

        string raw = File.ReadAllText(Config.LocalePath + "group_data");
        if (raw == "")
            return false;

        if (Mobs.Length == 0)
            return true;

        try
        {
            int len = getFinalLocaleId(raw);
            GroupData[] group_data = new GroupData[len];
            string[] lines = raw.Split(Environment.NewLine.ToCharArray());
            for (int l = 0; l < lines.Length; l++)
            {
                if (lines[l] == "" || lines[l][0] == '#')
                    continue;

                string[] line_contents = lines[l].Split('\t');

                Int32.TryParse(line_contents[0].ToString(), out int gid);
                string label = line_contents[1];
                string cleanMids = line_contents[2].Trim(new Char[] { '[', ']' });
                int[] mobs = Array.ConvertAll(cleanMids.Split(','), int.Parse);
                GroupData nGroupData = new GroupData(gid, label, mobs);
                group_data[gid] = nGroupData;
            }

            Config.MobGroups = group_data.ToArray();
        }
        catch (Exception e) { Logger.Syserr(e.Message); return false; }

        return true;
    }

    public static bool ReadMobData()
    {
        if (!File.Exists(Config.LocalePath + "mob_data"))
            return false;

        string raw = File.ReadAllText(Config.LocalePath + "mob_data");
        if (raw == "")
            return false;

        try
        {
            int len = getFinalLocaleId(raw);
            MobData[] mobs = new MobData[len];
            string[] lines = raw.Split(Environment.NewLine.ToCharArray());
            for (int l = 0; l < lines.Length; l++)
            {
                if (lines[l] == "" || lines[l][0] == '#')
                    continue;

                string[] line_contents = lines[l].Split('\t');
                Int32.TryParse(line_contents[0].ToString(), out int id);
                string name = line_contents[1];
                MOB_WALK_TYPE walk_type = (MOB_WALK_TYPE)Enum.Parse(typeof(MOB_WALK_TYPE), line_contents[2]);
                float wander_radius = float.Parse(line_contents[3].ToString());
                Int32.TryParse(line_contents[4].ToString(), out int wanderWait);
                float.TryParse(line_contents[5].ToString(), out float maxHp);
                float.TryParse(line_contents[6].ToString(), out float hpRegen);
                float.TryParse(line_contents[7].ToString(), out float movSpeed);
                float.TryParse(line_contents[8].ToString(), out float attSpeed);
                float.TryParse(line_contents[9].ToString(), out float pAttack);
                float.TryParse(line_contents[10].ToString(), out float mAttack);
                float.TryParse(line_contents[11].ToString(), out float pDef);
                float.TryParse(line_contents[12].ToString(), out float mDef);
                float.TryParse(line_contents[13].ToString(), out float attRange);

                MobStats nStats = new MobStats(walk_type, wander_radius, wanderWait, maxHp, hpRegen, attSpeed, movSpeed, pAttack, mAttack, pDef, mDef, attRange);
                MobData nData = new MobData(id, name, nStats);
                mobs[id] = nData;
            }

            Config.Mobs = mobs.ToArray();
        }
        catch(Exception e) { Logger.Syserr(e.Message); return false; }

        return true;
    }

    public static bool ReadItemData()
    {
        if (!File.Exists(Config.LocalePath + "item_data"))
            return false;

        string raw = File.ReadAllText(Config.LocalePath + "item_data");
        if (raw == "")
            return false;

        try
        {
            int len = getFinalLocaleId(raw);
            ItemData[] items = new ItemData[len];
            string[] lines = raw.Split(Environment.NewLine.ToCharArray());
            for (int l = 0; l < lines.Length; l++)
            {
                if (lines[l] == "" || lines[l][0] == '#')
                    continue;

                string[] line_contents = lines[l].Split('\t');
                Int32.TryParse(line_contents[0].ToString(), out int vnum);
                string name = line_contents[1];
                Int32.TryParse(line_contents[2].ToString(), out int level);

                List<PLAYER_RACES> races = new List<PLAYER_RACES>();
                if (line_contents[3].ToString() == "ALL")
                {
                    races = Enum.GetValues(typeof(PLAYER_RACES)).Cast<PLAYER_RACES>().Select(v => v.ToString()).ToList().Select(x => Enum.Parse(typeof(PLAYER_RACES), x)).Cast<PLAYER_RACES>().ToList();
                }
                else
                {
                    string[] races_string = line_contents[3].Split(',');
                    for (int r = 0; r < races_string.Length; r++)
                        races.Add((PLAYER_RACES)Enum.Parse(typeof(PLAYER_RACES), races_string[r]));
                }

                Int32.TryParse(line_contents[4].ToString(), out int size);
                Int32.TryParse(line_contents[5].ToString(), out int _stacks);
                bool stacks = false;
                if (_stacks == 1)
                    stacks = true;
                ITEM_TYPES type = (ITEM_TYPES)Enum.Parse(typeof(ITEM_TYPES), line_contents[6]);
                ITEM_SUB_TYPES sub_type = (ITEM_SUB_TYPES)Enum.Parse(typeof(ITEM_SUB_TYPES), line_contents[7]);
                BONUS_TYPE bonus_type0 = (BONUS_TYPE)Enum.Parse(typeof(BONUS_TYPE), line_contents[8]);
                float.TryParse(line_contents[9].ToString(), out float bonus_value0);
                BONUS_TYPE bonus_type1 = (BONUS_TYPE)Enum.Parse(typeof(BONUS_TYPE), line_contents[10]);
                float.TryParse(line_contents[11].ToString(), out float bonus_value1);
                BONUS_TYPE bonus_type2 = (BONUS_TYPE)Enum.Parse(typeof(BONUS_TYPE), line_contents[12]);
                float.TryParse(line_contents[13].ToString(), out float bonus_value2);
                BONUS_TYPE bonus_type3 = (BONUS_TYPE)Enum.Parse(typeof(BONUS_TYPE), line_contents[14]);
                float.TryParse(line_contents[15].ToString(), out float bonus_value3);
                BONUS_TYPE bonus_type4 = (BONUS_TYPE)Enum.Parse(typeof(BONUS_TYPE), line_contents[16]);
                float.TryParse(line_contents[17].ToString(), out float bonus_value4);
                BONUS_TYPE bonus_type5 = (BONUS_TYPE)Enum.Parse(typeof(BONUS_TYPE), line_contents[18]);
                float.TryParse(line_contents[19].ToString(), out float bonus_value5);

                ItemData iData = new ItemData(vnum, name, level, races.ToArray(), size, stacks, type, sub_type, bonus_type0, bonus_value0, bonus_type1, bonus_value1, bonus_type2, bonus_value2, bonus_type3, bonus_value3, bonus_type4, bonus_value4, bonus_type5, bonus_value5);
                items[vnum] = iData;
            }

            Config.Items = items.ToArray();
        }
        catch (Exception e) { Logger.Syserr(e.Message); return false; }

        return true;
    }

    private static int getFinalLocaleId(string contents)
    {
        int len = 1;
        string[] lines = contents.Split('\n');
        string[] line_contents = lines[lines.Length -1].Split('\t');
        Int32.TryParse(line_contents[0], out len);
        return len + 1;
    }

    public static bool TryReadConfigFiles()
    {
        bool validConfig = Config.ReadConfig();
        if (!validConfig)
        {
            Logger.Syserr("Server initialization aborted, error reading configuration.");
            return false;
        }
        return true;
    }

    public static bool TryReadGroupData()
    {
        bool validGroupData = Config.ReadGroupData();
        if(!validGroupData)
        {
            Logger.Syserr("Server initialization failed, group_data is invalid and could not be parsed.");
            return false;
        }

        return true;
    }

    public static bool TryReadMobData()
    {
        bool validMobData = Config.ReadMobData();
        if (!validMobData)
        {
            Logger.Syserr("Server initialization failed, mob_data is invalid and could not be parsed.");
            return false;
        }

        return true;
    }

    public static bool TryReadItemData()
    {
        bool validItemData = Config.ReadItemData();
        if (!validItemData)
        {
            Logger.Syserr("Server initialization failed, item_data is invalid and could not be parsed.");
            return false;
        }

        return true;
    }
}