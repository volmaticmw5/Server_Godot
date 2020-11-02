using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class DatabaseManager
{
    private Thread dbThread;
    private bool abort = false;
    private List<DatabaseAction> QueryQueue;
    private List<DatabaseActionUndefined> QueryUndefQueue;

    private MySqlConnection[] ConnectionPool;
    private int lastIdUsed = 0;
    private int reconnectAttemptCount = 1;
    private int maxReconnectAttempts = 3;
    private bool isOk = false;
    private int dbConnectedCount = 0;
    private int reconnectWait = 5;

    public DatabaseManager(int tick, int poolSize)
    {
        reconnectAttemptCount = 1;
        QueryQueue = new List<DatabaseAction>();
        QueryUndefQueue = new List<DatabaseActionUndefined>();
        ConnectionPool = new MySqlConnection[poolSize];
        for (int i = 0; i < poolSize; i++)
            ConnectionPool[i] = new MySqlConnection(@"server=" + Config.DatabaseHost + ";userid=" + Config.DatabaseUser + ";password=" + Config.DatabasePassword + ";database=" + Config.DatabaseDefault + "");
        _ = ConnectPool();

        if (!IsOK())
            return;

        this.dbThread = new Thread(new ThreadStart(() => ThreadWork(tick)));
        this.dbThread.Start();
    }

    private async Task ConnectPool()
    {
        for (int i = 0; i < ConnectionPool.Length; i++)
        {
            try { await ConnectionPool[i].OpenAsync(); Logger.Syslog($"Connected instance ({i}) to the database sucessfully..."); dbConnectedCount++; }
            catch (MySqlException ex)
            {
                Logger.Syserr($"Error: {ex.Message}");
                Logger.Syserr($"Error connecting to the database (Instance {i}). Will attempt to reconnect.. (Attempt 1/{maxReconnectAttempts})");
                Thread.Sleep(reconnectWait * 1000);
                _ = Reconnect(i);
            }
        }

        if (dbConnectedCount == ConnectionPool.Length)
        {
            Logger.Syslog("Finished initialization of database pool.");
            isOk = true;
        }

        return;
    }

    public async Task Reconnect(int instanceId)
    {
        try { await ConnectionPool[instanceId].OpenAsync(); Logger.Syslog($"Connected instance ({instanceId}) to the database sucessfully..."); }
        catch (MySqlException ex)
        {
            if (maxReconnectAttempts - 1 == reconnectAttemptCount)
            {
                reconnectAttemptCount = 1;
                Logger.Syserr($"Error connecting to the database (Instance {instanceId}, attempt {maxReconnectAttempts}/{maxReconnectAttempts}).");
            }
            else
            {
                reconnectAttemptCount++;
                Logger.Syserr($"Error connecting to the database (Instance {instanceId}). Will attempt to reconnect.. (Attempt {reconnectAttemptCount}/{maxReconnectAttempts})");
                _ = Reconnect(instanceId);
                Thread.Sleep(reconnectWait * 1000);
            }

            Logger.Syserr(ex.Message);
        }
    }

    public bool IsOK()
    {
        return this.isOk;
    }

    public async Task<MySqlConnection> GetSuitableConnection()
    {
        lastIdUsed++;
        if (lastIdUsed >= ConnectionPool.Length)
            lastIdUsed = 0;

        if (ConnectionPool[lastIdUsed].State == ConnectionState.Closed || ConnectionPool[lastIdUsed].State == ConnectionState.Broken)
            await ConnectionPool[lastIdUsed].OpenAsync();

        return ConnectionPool[lastIdUsed];
    }

    public void CloseConnectionPool(int poolId)
    {
        if (ConnectionPool[poolId].State == System.Data.ConnectionState.Open)
            ConnectionPool[poolId].Close();
    }

    public void CloseAllConnections()
    {
        for (int i = 0; i < ConnectionPool.Length; i++)
            CloseConnectionPool(i);
    }

    private void ThreadWork(int tick)
    {
        Logger.Syslog("Database thread started.");
        DateTime nextLoop = DateTime.Now;
        while (!abort)
        {
            DatabaseAction[] temp = QueryQueue.ToArray();
            DatabaseActionUndefined[] tempUndef = QueryUndefQueue.ToArray();
            QueryQueue.Clear();
            QueryUndefQueue.Clear();

            for (int i = 0; i < temp.Length; i++)
                temp[i].ExecuteQuery(temp[i].clientId);

            for (int i = 0; i < tempUndef.Length; i++)
                tempUndef[i].ExecuteQuery();

            nextLoop = nextLoop.AddMilliseconds(tick);
            if (nextLoop < DateTime.Now)
                Logger.Syserr($"Database thread hiched for {(DateTime.Now - nextLoop).Milliseconds}ms!");

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

    public async Task<DataTable> QueryAsync(string query, List<MySqlParameter> parameters)
    {
        // Sanitize 
        query = query.Replace("[[account]]", Config.DatabaseAccountDb);
        query = query.Replace("[[player]]", Config.DatabasePlayerDb);
        query = query.Replace("[[log]]", Config.DatabaseLogDb);

        DataTable result = new DataTable();
        try
        {
            MySqlCommand cmd = new MySqlCommand(query, await Server.DB.GetSuitableConnection());

            foreach (MySqlParameter param in parameters)
                cmd.Parameters.Add(param);

            using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
            {
                await da.FillAsync(result);
                return result;
            }
        }
        catch (MySqlException ex)
        {
            Logger.Syserr($"{ex.Message}");
            return null;
        }
    }

    public void AddQueryToQeue(string query, List<MySqlParameter> parameters, int client, Action<int, DataTable> returnMethod = null)
    {
        // Sanitize 
        query = query.Replace("[[account]]", Config.DatabaseAccountDb);
        query = query.Replace("[[player]]", Config.DatabasePlayerDb);
        query = query.Replace("[[log]]", Config.DatabaseLogDb);
        // Add to queue
        DatabaseAction nAction = new DatabaseAction(query, client, parameters, returnMethod);
        QueryQueue.Add(nAction);
    }

    public void AddQueryToQeueUndefined(string query, List<MySqlParameter> parameters)
    {
        // Sanitize 
        query = query.Replace("[[account]]", Config.DatabaseAccountDb);
        query = query.Replace("[[player]]", Config.DatabasePlayerDb);
        query = query.Replace("[[log]]", Config.DatabaseLogDb);
        // Add to queue
        DatabaseActionUndefined nAction = new DatabaseActionUndefined(query, parameters);
        QueryUndefQueue.Add(nAction);
    }

    public async Task<DataTable> QuerySync(string query, List<MySqlParameter> parameters)
    {
        // Sanitize 
        query = query.Replace("[[account]]", Config.DatabaseAccountDb);
        query = query.Replace("[[player]]", Config.DatabasePlayerDb);
        query = query.Replace("[[log]]", Config.DatabaseLogDb);

        DataTable result = new DataTable();
        try
        {
            MySqlCommand cmd = new MySqlCommand(query, await Server.DB.GetSuitableConnection());

            foreach (MySqlParameter param in parameters)
                cmd.Parameters.Add(param);

            using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
            {
                await da.FillAsync(result);
                return result;
            }
        }
        catch (MySqlException ex)
        {
            Logger.Syserr($"{ex.Message}");
        }
        return null;
    }

    public async Task<long> QuerySyncReturnAIAsync(string query, List<MySqlParameter> parameters)
    {
        // Sanitize 
        query = query.Replace("[[account]]", Config.DatabaseAccountDb);
        query = query.Replace("[[player]]", Config.DatabasePlayerDb);
        query = query.Replace("[[log]]", Config.DatabaseLogDb);

        DataTable result = new DataTable();
        try
        {
            MySqlCommand cmd = new MySqlCommand(query, await Server.DB.GetSuitableConnection());

            foreach (MySqlParameter param in parameters)
                cmd.Parameters.Add(param);

            cmd.ExecuteNonQuery();
            long id = cmd.LastInsertedId;
            return id;
        }
        catch (MySqlException ex)
        {
            Logger.Syserr($"{ex.Message}");
        }
        return -1;
    }
}