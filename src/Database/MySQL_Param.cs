using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

public class MySQL_Param
{
	/// <summary>
	/// Build MySqlParamater, clean up any illegal characters
	/// </summary>
	/// <param name="identifier"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	public static MySqlParameter Parameter(string identifier, object value)
	{
		Regex.Replace(identifier, @"\s+", "");
		return new MySqlParameter(identifier, value);
	}
}