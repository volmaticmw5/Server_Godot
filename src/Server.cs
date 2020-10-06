using System;
using System.Collections.Generic;
using System.Threading;

class Server
{
    public static DatabaseManager DB;
    private static bool isRunning = true;
    private static Thread mainThread;
    private static Thread mapThread;
    public static Core the_core;

    static void Main(string[] args)
    {
        bool canBoot = SetupServerForInitialization();
        if(!canBoot) Environment.Exit(1);
        BootServer();
    }

    private static bool SetupServerForInitialization()
    {
        if (!Config.TryReadConfigFiles()) return false;
        if (!TryConnectToDatabase()) return false;
        return true;
    }

    private static bool TryConnectToDatabase()
    {
        DB = new DatabaseManager(Config.DatabaseTick, Config.DatabasePoolSize);
        if (!DB.IsOK())
        {
            Logger.Syslog("Server initialization failed: database manager was not ok");
            return false;
        }
        return true;
    }

    private static void BootServer()
    {
        StartCorrectTypeOfServerCore();
        InitializeMainThread();
        InitializeMapThread();
    }

    private static void StartCorrectTypeOfServerCore()
    {
        if (Config.Type == ServerTypes.Authentication)
            Server.the_core = new AuthCore();
        else if (Config.Type == ServerTypes.Game)
            Server.the_core = new Core();
        else if (Config.Type == ServerTypes.Chat)
            Server.the_core = new ChatCore();

        Logger.Syslog($"{Config.Type.ToString()} server started on port {Config.Port}");
    }

    private static void InitializeMainThread()
    {
        List<Action> mainThreadActions = new List<Action>() { () => ThreadManager.UpdateMain() };
        mainThread = new Thread(new ThreadStart(() => ThreadedWork(mainThreadActions, "Main", Config.Tick)));
        mainThread.Start();
    }

    private static void InitializeMapThread()
    {
        if (Config.Type != ServerTypes.Game)
            return;

        MapManager.AddConfigMapsToMapManager();
        List<Action> mapThreadActions = new List<Action>() { () => ThreadManager.UpdateMapThread(), () => MapManager.Update() };
        mapThread = new Thread(new ThreadStart(() => ThreadedWork(mapThreadActions, "Map", Config.MapTick)));
        mapThread.Start();
    }

    private static void ThreadedWork(List<Action> actions, string threadName, int msTick)
    {
        Logger.Syslog($"[SERVER] {threadName} thread has started. Running at {msTick} ms per tick.");
        DateTime nextLoop = DateTime.Now;
        while (isRunning)
        {
            foreach (Action action in actions)
                action();

            nextLoop = nextLoop.AddMilliseconds(msTick);

            if (nextLoop < DateTime.Now)
                Logger.Syslog($"[SERVER] {threadName} thread hiched for {(DateTime.Now - nextLoop).Milliseconds}ms!");

            if (nextLoop > DateTime.Now)
            {
                TimeSpan time = (nextLoop - DateTime.Now);
                if (time < TimeSpan.Zero)
                    time = TimeSpan.Zero;
                if (time > TimeSpan.MaxValue)
                    time = TimeSpan.Zero;

                Thread.Sleep(time);
            }
        }
    }
}
