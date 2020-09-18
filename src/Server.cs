using System;
using System.Collections.Generic;
using System.Threading;

class Server
{
    public static DatabaseManager DB;
    private static bool isRunning = true;
    private static Thread mainThread;

    static void Main(string[] args)
    {
        int cResult = Config.ReadConfig();
        if(cResult != 0)
        {
            Logger.Syslog("Server initialization aborted, error reading configuration.");
            Environment.Exit(1);
        }

        // Initialize Database pool
        DB = new DatabaseManager(Config.DatabaseTick, Config.DatabasePoolSize);
        if (!DB.IsOK())
        {
            Logger.Syslog("Server initialization failed: database manager was not ok");
            return;
        }

        // Initialize threads
        List<Action> mainThreadActions = new List<Action>() { () => ThreadManager.UpdateMain(), () => ThreadManager.UpdateMain() };
        mainThread = new Thread(new ThreadStart(() => ThreadedWork(mainThreadActions, "Main", Config.Tick)));
        mainThread.Start();

        switch (Config.Type)
        {
            case ServerTypes.Authentication:
                // Start the core
                AuthCore auth_core = new AuthCore();

                // Print something
                Logger.Syslog($"{ServerTypes.Authentication.ToString()} server started on port {Config.Port}");

                break;
            case ServerTypes.Handler:

                break;
            case ServerTypes.Game:

                break;
        }
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
