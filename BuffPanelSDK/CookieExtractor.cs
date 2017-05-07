using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace BuffPanel
{
    public class CookieExtractor
    {
        public class CookieData
        {
            public string plainText { get; }
            public long expiresUTC { get; }

            public CookieData(string plainText, long expiresUTC)
            {
                this.plainText = plainText;
                this.expiresUTC = expiresUTC;
            }
        }

        public static Dictionary<string, string> ReadChromeCookies(string gameToken)
        {
            var result = getChromeCookies(gameToken);
            var temp = getFirefoxCookies(gameToken);
            foreach (var x in temp)
            {
                result[x.Key] = x.Value;
            }
            var temp2 = getEdgeCookies(gameToken);
            foreach (var x in temp2)
            {
                result[x.Key] = x.Value;
            }
            return result;
        }

        private static Dictionary<string, string> getEdgeCookies(string gameToken)
        {
            var cookieStores = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Packages\Microsoft.MicrosoftEdge_8wekyb3d8bbwe\", "*.cookie", SearchOption.AllDirectories);
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (var cookieStorePath in cookieStores)
            {
                var text = File.ReadAllLines(cookieStorePath);
                if (text[2].Contains("trbt.it"))
                {
                    result.Add(text[0], text[1]);
                }
            }
            return result;
        }

        private static Dictionary<string, string> getFirefoxCookies(string gameToken)
        {
            var cookieStores = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Mozilla\Firefox\Profiles\", "cookies.sqlite", SearchOption.AllDirectories);
            //string dbPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\Default\Cookies";
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (var cookieStorePath in cookieStores)
            {
                Console.WriteLine(cookieStorePath);
                if (!File.Exists(cookieStorePath))
                {
                    continue;
                }
                IDbConnection connection = new SQLiteConnection("URI=file:" + cookieStorePath);
                connection.Open();
                IDbCommand command = connection.CreateCommand();
                command.CommandText = "SELECT name, value FROM moz_cookies WHERE host LIKE '%" + gameToken + "" + BuffPanel.redirectURI + "%';";
                IDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    //var encryptedData = (byte[])reader[1];
                    //var decodedData = System.Security.Cryptography.ProtectedData.Unprotect(encryptedData, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                    //var plainText = System.Text.Encoding.ASCII.GetString(decodedData); // Looks like ASCII
                    var clickId = reader.GetString(0);
                    var campaignId = reader.GetString(1);
                    result.Add(clickId, campaignId);
                }

                connection.Close();
            }
            return result;
        }

        private static Dictionary<string, string> getChromeCookies(string gameToken)
        {
            var cookieStores = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\", "Cookies", SearchOption.AllDirectories);
            //string dbPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\Default\Cookies";
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (var cookieStorePath in cookieStores)
            {
                Console.WriteLine(cookieStorePath);
                if (!File.Exists(cookieStorePath))
                {
                    continue;
                }
                IDbConnection connection = new SQLiteConnection("URI=file:" + cookieStorePath);
                connection.Open();
                IDbCommand command = connection.CreateCommand();
                command.CommandText = "SELECT name, encrypted_value FROM cookies WHERE host_key LIKE '%" + gameToken + "" + BuffPanel.redirectURI + "%';";
                IDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var encryptedData = (byte[])reader[1];
                    var decodedData = System.Security.Cryptography.ProtectedData.Unprotect(encryptedData, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                    var plainText = System.Text.Encoding.ASCII.GetString(decodedData); // Looks like ASCII
                    var clickId = reader.GetString(0);
                    result.Add(clickId, plainText);
                }

                connection.Close();
            }
            return result;
        }
    }
}
