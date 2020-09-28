using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

public class Security
{
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

    internal static bool ValidatePacket(int id, int fromClient, int session_id, bool auth = true)
    {
        if (id == fromClient)
        {
            if(auth)
            {
                if (AuthCore.Clients[fromClient] != null)
                {
                    if (AuthCore.Clients[fromClient].getSessionId() == session_id)
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (Core.Clients[fromClient] != null)
                {
                    if ((int)Core.Clients[fromClient].session_id == session_id)
                    {
                        return true;
                    }
                }
            }
        }
        else
        {
            Logger.Syslog($"Expected client id of {id} but got {fromClient}!!!");
        }

        Logger.Syslog($"Failed to validate client, disconnecting client #{fromClient} | Expecting sid of {Core.Clients[fromClient].session_id} and got {session_id}");
        if (auth)
            AuthCore.Clients[fromClient].getTcp().Disconnect();
        else
            Core.Clients[fromClient].getTcp().Disconnect();
        return false;
    }
}