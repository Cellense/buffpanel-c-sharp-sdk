using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace BuffPanel
{
    public class CookieExtractor
    {
        public static Dictionary<string, object> ReadChromeCookies(string gameToken)
        {
            int i = 0;
            string dbPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\Default\Cookies";
            if (!System.IO.File.Exists(dbPath))
            {
                throw new System.IO.FileNotFoundException("Cant find cookie store", dbPath); // race condition, but i'll risk it
            }

            IDbConnection connection = new SQLiteConnection("URI=file:" + dbPath);
            connection.Open();
            IDbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT name, encrypted_value FROM cookies WHERE host_key LIKE '%" + gameToken + "" + BuffPanel.redirectURI + "%';";
            IDataReader reader = command.ExecuteReader();

            Dictionary<string, object> result = new Dictionary<string, object>();
            while (reader.Read())
            {
                var encryptedData = (byte[])reader[1];
                var decodedData = System.Security.Cryptography.ProtectedData.Unprotect(encryptedData, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                var plainText = System.Text.Encoding.ASCII.GetString(decodedData); // Looks like ASCII
                result.Add(i.ToString() + "~" + reader.GetString(0), plainText);
            }

            connection.Close();
            i++;
            while (true) { 
                dbPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\Profile " + i + @"\Cookies";
                if (!System.IO.File.Exists(dbPath))
                {
                    break;
                }

                connection = new SQLiteConnection("URI=file:" + dbPath);
                connection.Open();
                command = connection.CreateCommand();
                command.CommandText = "SELECT name, encrypted_value FROM cookies WHERE host_key LIKE '%" + gameToken + "" + BuffPanel.redirectURI + "%';";
                reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var encryptedData = (byte[])reader[1];
                    var decodedData = System.Security.Cryptography.ProtectedData.Unprotect(encryptedData, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                    var plainText = System.Text.Encoding.ASCII.GetString(decodedData); // Looks like ASCII
                    result.Add(i.ToString() + "~" + reader.GetString(0), plainText);
                }

                connection.Close();
                i++;
            }

            return result;
        }
    }
}
