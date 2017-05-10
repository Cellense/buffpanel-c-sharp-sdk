using BuffPanel.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;

namespace BuffPanel
{
    public class CookieExtractor
    {
        public static Dictionary<string, string> ReadCookies(string gameToken, Assembly sql, Assembly prot, Logger logger = null)
        {
            var currentDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Path.Combine(Path.GetTempPath(), @"BuffPanel\"));
            Logger innerLogger = (logger != null) ? logger : new NullLogger();

            var result = new Dictionary<string, string>();
            var chrome = getChromeCookies(gameToken, sql, prot, innerLogger);
            foreach (var x in chrome)
            {
                result[x.Key] = x.Value;
            }
            var firefox = getFirefoxCookies(gameToken, sql, innerLogger);
            foreach (var x in firefox)
            {
                result[x.Key] = x.Value;
            }
            var edge = getEdgeCookies(gameToken, innerLogger);
            foreach (var x in edge)
            {
                result[x.Key] = x.Value;
            }
            var IEWin7 = getIEWin7Cookies(gameToken, innerLogger);
            foreach (var x in IEWin7)
            {
                result[x.Key] = x.Value;
            }
            var IEWin8 = getIEWin8Cookies(gameToken, innerLogger);
            foreach (var x in IEWin8)
            {
                result[x.Key] = x.Value;
            }
            var IEWin10 = getIEWin10Cookies(gameToken, innerLogger);
            foreach (var x in IEWin10)
            {
                result[x.Key] = x.Value;
            }
            Directory.SetCurrentDirectory(currentDir);
            return result;
        }

        private static Dictionary<string, string> getChromeCookies(string gameToken, Assembly sqllite, Assembly protecteddata, Logger logger = null)
        {
            Logger innerLogger = (logger != null) ? logger : new NullLogger();
            var chromePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\";
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (!Directory.Exists(chromePath))
            {
                innerLogger.Log(Level.Warn, "No Google Chrome cookies.");
                return result;
            }
            var cookieStores = Directory.GetFiles(chromePath, "Cookies", SearchOption.AllDirectories);
            foreach (var cookieStorePath in cookieStores)
            {
                innerLogger.Log(Level.Debug, cookieStorePath);
                if (!File.Exists(cookieStorePath))
                {
                    continue;
                }
                Type conn = sqllite.GetType("System.Data.SQLite.SQLiteConnection");
                IDbConnection connection = Activator.CreateInstance(conn, new object[] { "URI=file:" + cookieStorePath }) as IDbConnection;
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

        private static Dictionary<string, string> getFirefoxCookies(string gameToken, Assembly sqllite, Logger logger = null)
        {
            Logger innerLogger = (logger != null) ? logger : new NullLogger();
            var firefoxPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Mozilla\Firefox\Profiles\";
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (!Directory.Exists(firefoxPath))
            {
                innerLogger.Log(Level.Warn, "No Mozilla Firefox cookies.");
                return result;
            }
            var cookieStores = Directory.GetFiles(firefoxPath, "cookies.sqlite", SearchOption.AllDirectories);
            foreach (var cookieStorePath in cookieStores)
            {
                innerLogger.Log(Level.Debug, cookieStorePath);
                if (!File.Exists(cookieStorePath))
                {
                    continue;
                }
                Type conn = sqllite.GetType("System.Data.SQLite.SQLiteConnection");
                IDbConnection connection = Activator.CreateInstance(conn, new object[] { "URI=file:" + cookieStorePath }) as IDbConnection;
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

        private static Dictionary<string, string> getEdgeCookies(string gameToken, Logger logger = null)
        {
            Logger innerLogger = (logger != null) ? logger : new NullLogger();
            var edgePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Packages\Microsoft.MicrosoftEdge_8wekyb3d8bbwe\";
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (!Directory.Exists(edgePath))
            {
                innerLogger.Log(Level.Warn, "No Microsoft Edge cookies.");
                return result;
            }
            var cookieStores = Directory.GetFiles(edgePath, "*.cookie", SearchOption.AllDirectories);
            foreach (var cookieStorePath in cookieStores)
            {
                var text = File.ReadAllLines(cookieStorePath);
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i].Contains(gameToken + "" + BuffPanel.redirectURI))
                    {
                        innerLogger.Log(Level.Debug, cookieStorePath);
                        result.Add(text[i - 2], text[i - 1]);
                    }
                }
            }
            return result;
        }

        private static Dictionary<string, string> getIEWin7Cookies(string gameToken, Logger logger = null)
        {
            Logger innerLogger = (logger != null) ? logger : new NullLogger();
            var IEPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft\Windows\Cookies\";
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (!Directory.Exists(IEPath))
            {
                innerLogger.Log(Level.Warn, "No Internet Explorer Windows 7 cookies.");
                return result;
            }
            var cookieStores = Directory.GetFiles(IEPath, "*.txt", SearchOption.AllDirectories);
            foreach (var cookieStorePath in cookieStores)
            {
                var text = File.ReadAllLines(cookieStorePath);
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i].Contains(gameToken + "" + BuffPanel.redirectURI))
                    {
                        innerLogger.Log(Level.Debug, cookieStorePath);
                        result.Add(text[i - 2], text[i - 1]);
                    }
                }
            }
            return result;
        }

        private static Dictionary<string, string> getIEWin8Cookies(string gameToken, Logger logger = null)
        {
            Logger innerLogger = (logger != null) ? logger : new NullLogger();
            var IEPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Windows\INetCookies\";
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (!Directory.Exists(IEPath))
            {
                innerLogger.Log(Level.Warn, "No Internet Explorer Windows 8 cookies.");
                return result;
            }
            var cookieStores = Directory.GetFiles(IEPath, "*.txt", SearchOption.AllDirectories);
            foreach (var cookieStorePath in cookieStores)
            {
                var text = File.ReadAllLines(cookieStorePath);
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i].Contains(gameToken + "" + BuffPanel.redirectURI))
                    {
                        innerLogger.Log(Level.Debug, cookieStorePath);
                        result.Add(text[i - 2], text[i - 1]);
                    }
                }
            }
            return result;
        }

        private static Dictionary<string, string> getIEWin10Cookies(string gameToken, Logger logger = null)
        {
            Logger innerLogger = (logger != null) ? logger : new NullLogger();
            var IEPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Windows\INetCookies\";
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (!Directory.Exists(IEPath))
            {
                innerLogger.Log(Level.Warn, "No Internet Explorer Windows 10 cookies.");
                return result;
            }
            var cookieStores = Directory.GetFiles(IEPath, "*.cookie", SearchOption.AllDirectories);
            foreach (var cookieStorePath in cookieStores)
            {
                var text = File.ReadAllLines(cookieStorePath);
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i].Contains(gameToken + "" + BuffPanel.redirectURI))
                    {
                        innerLogger.Log(Level.Debug, cookieStorePath);
                        result.Add(text[i - 2], text[i - 1]);
                    }
                }
            }
            return result;
        }
    }
}
