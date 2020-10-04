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
        bool validConfig = Config.ReadConfig();
        if(!validConfig)
        {
            Logger.Syslog("Server initialization aborted, error reading configuration.");
            Environment.Exit(1);
        }

        DB = new DatabaseManager(Config.DatabaseTick, Config.DatabasePoolSize);
        if (!DB.IsOK())
        {
            Logger.Syslog("Server initialization failed: database manager was not ok");
            Environment.Exit(1);
        }

        if (Config.Type == ServerTypes.Authentication)
            Server.the_core = new AuthCore();
        else
            Server.the_core = new Core();

        Logger.Syslog($"{Config.Type.ToString()} server started on port {Config.Port}");

        if(Config.Type == ServerTypes.Game)
        {
            foreach (MapStruct map in Config.Maps)
            {
                Map nMap = new Map(map.id, map.name);
                MapManager.AddMapToManager(nMap);
            }
            List<Action> mapThreadActions = new List<Action>() { () => MapManager.Tick() };
            mapThread = new Thread(new ThreadStart(() => ThreadedWork(mapThreadActions, "Map", Config.MapTick)));
            mapThread.Start();
        }

        // Initialize main thread
        List<Action> mainThreadActions = new List<Action>() { () => ThreadManager.UpdateMain() };
        mainThread = new Thread(new ThreadStart(() => ThreadedWork(mainThreadActions, "Main", Config.Tick)));
        mainThread.Start();
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
