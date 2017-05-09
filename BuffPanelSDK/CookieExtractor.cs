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

        public static Dictionary<string, string> ReadCookies(string gameToken)
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
            var temp3 = getIEWin7Cookies(gameToken);
            foreach (var x in temp3)
            {
                result[x.Key] = x.Value;
            }
            var temp4 = getIEWin8Cookies(gameToken);
            foreach (var x in temp4)
            {
                result[x.Key] = x.Value;
            }
            var temp5 = getIEWin10Cookies(gameToken);
            foreach (var x in temp5)
            {
                result[x.Key] = x.Value;
            }
            return result;
        }

        private static Dictionary<string, string> getEdgeCookies(string gameToken)
        {
            var edgePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Packages\Microsoft.MicrosoftEdge_8wekyb3d8bbwe\";
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (!Directory.Exists(edgePath))
                return result;
            var cookieStores = Directory.GetFiles(edgePath, "*.cookie", SearchOption.AllDirectories);
            foreach (var cookieStorePath in cookieStores)
            {
                var text = File.ReadAllLines(cookieStorePath);
                if (text[2].Contains(gameToken + "" + BuffPanel.redirectURI))
                {
                    Console.WriteLine(cookieStorePath);
                    result.Add(text[0], text[1]);
                }
            }
            return result;
        }

        private static Dictionary<string, string> getFirefoxCookies(string gameToken)
        {
            var firefoxPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Mozilla\Firefox\Profiles\";
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (!Directory.Exists(firefoxPath))
                return result;
            var cookieStores = Directory.GetFiles(firefoxPath, "cookies.sqlite", SearchOption.AllDirectories);
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
            var chromePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\";
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (!Directory.Exists(chromePath)) {
                Console.WriteLine("user data does not exist");
                return result;
            }
            var cookieStores = Directory.GetFiles(chromePath, "Cookies", SearchOption.AllDirectories);
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

        private static Dictionary<string, string> getIEWin7Cookies(string gameToken)
        {
            var iePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft\Windows\Cookies\";
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (!Directory.Exists(iePath))
                return result;
            var cookieStores = Directory.GetFiles(iePath, "*.txt", SearchOption.AllDirectories);
            foreach (var cookieStorePath in cookieStores)
            {
                var text = File.ReadAllLines(cookieStorePath);
                if (text[2].Contains(gameToken + "" + BuffPanel.redirectURI))
                {
                    Console.WriteLine(cookieStorePath);
                    result.Add(text[0], text[1]);
                }
            }
            return result;
        }

        private static Dictionary<string, string> getIEWin8Cookies(string gameToken)
        {
            var iePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Windows\INetCookies\";
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (!Directory.Exists(iePath))
                return result;
            var cookieStores = Directory.GetFiles(iePath, "*.txt", SearchOption.AllDirectories);
            foreach (var cookieStorePath in cookieStores)
            {
                var text = File.ReadAllLines(cookieStorePath);
                for (int i = 0; i < text.Length; i++) { 
                    if (text[i].Contains(gameToken + "" + BuffPanel.redirectURI))
                    {
                        Console.WriteLine(cookieStorePath);
                        result.Add(text[i-2], text[i-1]);
                    }
                }
            }
            return result;
        }

        private static Dictionary<string, string> getIEWin10Cookies(string gameToken)
        {
            var iePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Windows\INetCookies\";
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (!Directory.Exists(iePath))
                return result;
            var cookieStores = Directory.GetFiles(iePath, "*.cookie", SearchOption.AllDirectories);
            foreach (var cookieStorePath in cookieStores)
            {
                var text = File.ReadAllLines(cookieStorePath);
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i].Contains(gameToken + "" + BuffPanel.redirectURI))
                    {
                        Console.WriteLine(cookieStorePath);
                        result.Add(text[i - 2], text[i - 1]);
                    }
                }
            }
            return result;
        }
    }
}
