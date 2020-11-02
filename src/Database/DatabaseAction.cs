using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

class DatabaseAction
{
    public int clientId;
    public string Query;
    public Action<int, DataTable> ReturnMethod;
    public List<MySqlParameter> parameters;

    public DatabaseAction(string query, int clientId, List<MySqlParameter> parameters, Action<int, DataTable> returnMethod)
    {
        this.clientId = clientId;
        this.Query = query;
        this.parameters = parameters;
        this.ReturnMethod = returnMethod;
    }

    public async void ExecuteQuery(int client)
    {
        DataTable result = new DataTable();
        try
        {
            MySqlCommand cmd = new MySqlCommand(Query, await Server.DB.GetSuitableConnection());

            foreach (MySqlParameter param in parameters)
                cmd.Parameters.Add(param);

            if (ReturnMethod != null)
            {
                using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                    da.Fill(result);

                ReturnMethod(client, result);
            }
            else
            {
                await Task.Run(() => QueryAsync(cmd));
            }
        }
        catch (MySqlException ex)
        {
            Logger.Syserr($"{ex.Message}");
        }
    }

    private async Task<DataTable> QueryAsync(MySqlCommand cmd)
    {
        try
        {
            DataTable result = new DataTable();
            using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
            {
                await da.FillAsync(result);
                return result;
            }
        }
        catch { Logger.Syserr($"A query failed to execute! Command: {cmd}"); return null; }
    }
}