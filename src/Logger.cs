using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

class Logger
{
    public static void Syslog(string message)
    {
        if (Config.WriteToConsole)
            Console.WriteLine("[SYSLOG] " + DateTime.Now.ToString() + " " + message);

        using (FileStream fs = new FileStream("log/syslog", FileMode.Append, FileAccess.Write, FileShare.Write))
        {
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.WriteLine(DateTime.Now.ToString() + " " + message);
                sw.Flush();
                sw.Close();
            }
            fs.Close();
        }
    }

    public static void Syserr(string message)
    {
        if (Config.WriteToConsole)
            Console.WriteLine("[SYSERR] " + DateTime.Now.ToString() + " " + message);

        using (FileStream fs = new FileStream("log/syserr", FileMode.Append, FileAccess.Write, FileShare.Write))
        {
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.WriteLine(DateTime.Now.ToString() + " " + message);
                sw.Flush();
                sw.Close();
            }
            fs.Close();
        }
    }

    public static void CleanLogs()
    {
        if (File.Exists("log/syslog"))
            File.Delete("log/syslog");
        if (File.Exists("log/syserr"))
            File.Delete("log/syserr");
    }

    public static void ItemLog(int vnum, long iid, string action)
    {
        if (!Config.DbLogsEnabled)
            return;

        List<MySqlParameter> _params = new List<MySqlParameter>()
        {
            MySQL_Param.Parameter("?vnum", vnum),
            MySQL_Param.Parameter("?iid", iid),
            MySQL_Param.Parameter("?action", action),
        };
        Server.DB.AddQueryToQeueUndefined("INSERT INTO [[log]].item_log (vnum,iid,action,date) VALUES (?vnum,?iid,?action,NOW())", _params);
    }

    public static void PlayerLog(int pid, string what)
    {
        if (!Config.DbLogsEnabled)
            return;

        List<MySqlParameter> _params = new List<MySqlParameter>()
        {
            MySQL_Param.Parameter("?pid", pid),
            MySQL_Param.Parameter("?what", what),
        };
        Server.DB.AddQueryToQeueUndefined("INSERT INTO [[log]].player_log (pid,what,date) VALUES (?pid,?what,NOW())", _params);
    }
}