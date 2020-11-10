using System;
using System.Collections.Generic;
using System.Threading;

class Server
{
    public static DatabaseManager DB;
    public static Core the_core;
    public static TheadHelper main_thread_manager;
    public static TheadHelper map_thread_manager;
    public static TheadHelper player_thread_manager;

    static void Main(string[] args)
    {
        bool canBoot = SetupServerForInitialization();
        if(!canBoot) Environment.Exit(1);
        AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
        BootServer();
    }

    private static bool SetupServerForInitialization()
    {
        Logger.CleanLogs();
        if (!Config.TryReadConfigFiles()) return false;
        if (!Config.TryReadMobData()) return false;
        if (!Config.TryReadGroupData()) return false;
        if (!Config.TryReadItemData()) return false;
        if (!TryConnectToDatabase()) return false;
        return true;
    }

    static void OnProcessExit(object sender, EventArgs e)
    {
        Logger.Syslog("Exit signal received, started shutdown sequence.");
        if(Config.Type == ServerTypes.Game)
        {
            Logger.Syslog("Flushing core...");
            the_core.Flush();
            Logger.Syslog("Waiting for sql completion...");
            Thread.Sleep(2500);
            Logger.Syslog("Core flushed.");
        }

        DB.CloseAllConnections();
        Logger.Syslog("Closed database connections");
        main_thread_manager.abort = true;
        Thread.Sleep(200);
        Logger.Syslog("Main thread aborted.");
        map_thread_manager.abort = true;
        Thread.Sleep(200);
        Logger.Syslog("Map thread aborted.");
        player_thread_manager.abort = true;
        Thread.Sleep(200);
        Logger.Syslog("Player thread aborted.");

        Logger.Syslog("Shutting down sequence completed. Bye!");
    }

    private static bool TryConnectToDatabase()
    {
        DB = new DatabaseManager(Config.DatabaseTick, Config.DatabasePoolSize);
        if (!DB.IsOK())
        {
            Logger.Syserr("Server initialization failed: database manager was not ok");
            return false;
        }
        return true;
    }

    private static void BootServer()
    {
        StartCorrectTypeOfServerCore();
        InitializeMainThread();
        InitializeMapThread();
        InitializePlayerThread();
    }

    private static void StartCorrectTypeOfServerCore()
    {
        if (Config.Type == ServerTypes.Authentication)
            Server.the_core = new AuthCore();
        else if (Config.Type == ServerTypes.Game)
            Server.the_core = new Core();

        Logger.Syslog($"{Config.Type.ToString()} server started on port {Config.Port}");
    }

    private static void InitializeMainThread()
    {
        main_thread_manager = new TheadHelper();
        main_thread_manager.the_thread.Start();
    }

    private static void InitializeMapThread()
    {
        if (Config.Type != ServerTypes.Game)
            return;

        MapManager.AddConfigMapsToMapManager();
        map_thread_manager = new TheadHelper(new List<Action>(){() => MapManager.Update() });
        map_thread_manager.the_thread.Start();
    }

    private static void InitializePlayerThread()
    {
        if (Config.Type != ServerTypes.Game)
            return;

        player_thread_manager = new TheadHelper(new List<Action>() { () => PlayerManager.Update() });
        player_thread_manager.the_thread.Start();
    }
}
