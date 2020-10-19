using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public class Security
{
    public enum SessionErrorCodes
    {
        NON_EXISTENT = 1,
        VALID = 2,
    }

    public static byte[] GetSalt()
    {
        return Encoding.ASCII.GetBytes("gJUF6ZvYNSG2PwvJfBCBT3hx");
    }

    public static byte[] Hash(string value, byte[] salt)
    {
        return Hash(Encoding.UTF8.GetBytes(value), salt);
    }

    public static byte[] Hash(byte[] value, byte[] salt)
    {
        byte[] saltedValue = value.Concat(salt).ToArray();
        return new SHA256Managed().ComputeHash(saltedValue);
    }

    public static bool Verify(byte[] password, byte[] hashedPassword)
    {
        byte[] passwordHash = Hash(password, GetSalt());
        return passwordHash.SequenceEqual(passwordHash);
    }

    public static bool ReceivedIdMatchesClientId(int id, int fromClient)
    {
        if (id == fromClient)
            return true;
        else
            return false;
    }

    public static bool Validate(int id, int fromClient, int session_id)
    {
        if (!ValidatePacket(id, fromClient, session_id))
        {
            Logger.Syslog($"Client #{id} failed to get validated and will be disconnected.");
            Server.the_core.Clients[fromClient].tcp.Disconnect();
            return false;
        }

        return true;
    }

    private static bool ValidatePacket(int cid, int fromClient, int session_id)
    {
        if(Config.Type == ServerTypes.Authentication)
        {
            AuthCore core = ((AuthCore)Server.the_core);
            if (cid != fromClient)
                return false;
            if (core.Clients == null)
                return false;
            if (core.Clients[fromClient] == null)
                return false;
            if (core.Clients[fromClient].session_id != session_id)
                return false;
            return true;
        }
            
        if (cid != fromClient)
            return false;
        if (Server.the_core.Clients == null)
            return false;
        if (Server.the_core.Clients[fromClient] == null)
            return false;
        if (Server.the_core.Clients[fromClient].session_id != session_id)
            return false;

        return true;
    }

    public static async Task<SessionErrorCodes> VerifySessionInDatabase(int cid, int session)
    {
        List<MySqlParameter> sessParams = new List<MySqlParameter>()
        {
            MySQL_Param.Parameter("?session", Server.the_core.Clients[cid].session_id),
            MySQL_Param.Parameter("?aid", Server.the_core.Clients[cid].aid)
        };
        DataTable result = await Server.DB.QueryAsync("SELECT COUNT(*) as count FROM [[player]].sessions WHERE `session`=?session AND `aid`=?aid LIMIT 1", sessParams);
        Int32.TryParse(result.Rows[0]["count"].ToString(), out int count);
        if (count == 0)
            return SessionErrorCodes.NON_EXISTENT;

        return SessionErrorCodes.VALID;
    }

    public static async Task<bool> isGM(int pid)
    {
        List<MySqlParameter> _params = new List<MySqlParameter>()
        {
            MySQL_Param.Parameter("?pid", pid),
        };
        DataTable result = await Server.DB.QueryAsync("SELECT COUNT(*) as count FROM [[player]].PLAYER WHERE `id`=?pid LIMIT 1", _params);
        if (result.Rows[0]["count"].ToString() == "1")
            return true;
        return false;
    }
}