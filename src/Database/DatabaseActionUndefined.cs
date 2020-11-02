using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

class DatabaseActionUndefined
{
    public string Query;
    public List<MySqlParameter> parameters;

    public DatabaseActionUndefined(string query, List<MySqlParameter> parameters)
    {
        this.Query = query;
        this.parameters = parameters;
    }

    public async void ExecuteQuery()
    {
        DataTable result = new DataTable();
        try
        {
            MySqlCommand cmd = new MySqlCommand(Query, await Server.DB.GetSuitableConnection());

            foreach (MySqlParameter param in parameters)
                cmd.Parameters.Add(param);

             await Task.Run(() => QueryAsync(cmd));
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