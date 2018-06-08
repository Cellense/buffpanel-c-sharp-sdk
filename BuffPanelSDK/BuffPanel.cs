using System;
using System.Collections.Generic;
using System.Threading;
using BuffPanel.Logging;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace BuffPanel
{
    public class BuffPanel
    {
        internal class BuffPanelException : Exception
        {
            public BuffPanelException(string message) : base(message)
            {
            }
        }

        private static int requestTimeout = 10000;
        private static int baseRetryTimeout = 200;
        private static int maxRetries = 10;

        private static string serviceHostname = "staging.api.buffpanel.com";
        private static string servicePath = "/run_event/create";

        public static string version = "csharp_0.0.1";

        private static Thread worker = null;
        private static BuffPanel instance = null;

        private string url;
        private string httpBody;
        private Logger logger;

        public static void Track(string gameToken, bool isExistingPlayer,Logger logger = null)
        {
            Logger innerLogger = (logger != null) ? logger : new NullLogger();
            
            if (instance == null)
            {
                string playerToken = "";
                try
                {
                    playerToken = GetPlayerToken(gameToken);
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine("Exception: " + e.ToString());

                    playerToken = "unknown_player";
                }
                Console.Out.WriteLine("Player token: " + playerToken);

                string httpBody = Json.Serialize(new Dictionary<string, object> {
                    { "game_token", gameToken },
                    { "player_token", playerToken },
                    { "is_existing_player", isExistingPlayer},
                    { "version", version }
                });
                if (httpBody == null)
                {
                    innerLogger.Log(Level.Warn, "No suitable player token has been supplied.");
                    return;
                }

                instance = new BuffPanel("http://" + serviceHostname + servicePath, httpBody, innerLogger);
                worker = new Thread(new ThreadStart(instance.SendRequest));
                worker.Start();
            }
            else
            {
                innerLogger.Log(Level.Warn, "An instance is already running.");
            }
        }

        public static void Track(string gameToken, bool isExistingPlayer, Dictionary<string, string> attributes, Logger logger = null)
        {
            Logger innerLogger = (logger != null) ? logger : new NullLogger();

            if (instance == null)
            {
                string playerToken = "";
                try
                {
                    playerToken = GetPlayerToken(gameToken);
                }
                catch (Exception)
                {
                    playerToken = "unknown_player";
                }

                string httpBody = Json.Serialize(new Dictionary<string, object> {
                    { "game_token", gameToken },
                    { "player_token", playerToken },
                    { "is_existing_player", isExistingPlayer},
                    { "attributes", attributes},
                    { "version", version }
                });
                if (httpBody == null)
                {
                    innerLogger.Log(Level.Warn, "No suitable player token has been supplied.");
                    return;
                }

                instance = new BuffPanel("http://" + serviceHostname + servicePath, httpBody, innerLogger);
                worker = new Thread(new ThreadStart(instance.SendRequest));
                worker.Start();
            }
            else
            {
                innerLogger.Log(Level.Warn, "An instance is already running.");
            }
        }

        public static void Terminate()
        {
            worker.Join();
            worker = null;
            instance = null;
        }

        private BuffPanel(string newUrl, string newHttpBody, Logger newLogger)
        {
            this.url = newUrl;
            this.httpBody = newHttpBody;
            this.logger = newLogger;
        }

        private void SendRequest()
        {
            int currentTimeout = baseRetryTimeout;

            for (int i = 0; i < maxRetries; ++i)
            {
                try
                {
                    WebRequest request = CreateRequest();
                    var httpResponse = (HttpWebResponse)request.GetResponse();

                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();
                        if (result == null)
                        {
                            throw new BuffPanelException("The result cannot be read.");
                        }
                        var resultParse = Json.Deserialize(result) as Dictionary<string, object>;
                        if (resultParse == null)
                        {
                            throw new BuffPanelException("The response cannot be parsed.");
                        }

                        if (!(bool)resultParse["success"])
                        {
                            if (this.logger.IsLevelEnabled(Level.Error))
                            {
                                this.logger.Log(Level.Error, "An error has occured : \n" + result);
                            }

                            break;
                        }
                    }

                    break;
                }
                catch (WebException ex)
                {
                    if (this.logger.IsLevelEnabled(Level.Error))
                    {
                        this.logger.Log(Level.Error, ex.Message);
                    }
                }
                catch (BuffPanelException ex)
                {
                    if (this.logger.IsLevelEnabled(Level.Error))
                    {
                        this.logger.Log(Level.Error, ex.Message);
                    }
                }

                Thread.Sleep(currentTimeout);
                currentTimeout *= 2;
            }
        }

        private WebRequest CreateRequest()
        {

            WebRequest request = WebRequest.Create(this.url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Timeout = requestTimeout;

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                streamWriter.Write(this.httpBody);
                streamWriter.Flush();
                streamWriter.Close();
            }

            return request;
        }


        private static string GetUuidPersistPath()
        {
            OperatingSystem os = Environment.OSVersion;
            PlatformID platform = os.Platform;
            switch (platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\BuffPanel\";
                case PlatformID.Unix:
                    return Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) + @"/BuffPanel/";
                case PlatformID.MacOSX:
                    return Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) + @"/BuffPanel/";
                default:
                    return "";
            }
        }

        private static string ReadSavedUuid(string path)
        {
            if (File.Exists(path))
            {
                string uuid;
                try
                {
                    uuid = System.IO.File.ReadAllText(path);
                }
                catch (UnauthorizedAccessException)
                {
                    return "anonymous";
                }
                catch (Exception)
                {
                    return "";
                }
                if (!IsValidUuid(uuid))
                    return "";

                return uuid;
            }
            return "";
        }
        private static void SaveUuid(string filePath, string folderPath, string uuid)
        {
            System.IO.Directory.CreateDirectory(@folderPath);
            System.IO.File.WriteAllText(@filePath, uuid);
        }

        private static string GetPlayerToken(string gameToken)
        {
            string folderPath = GetUuidPersistPath();
            string filePath = folderPath + "uuid_" + gameToken;
            string uuid = ReadSavedUuid(filePath);
            if (string.IsNullOrEmpty(uuid))
            {
                uuid = System.Guid.NewGuid().ToString("D").ToUpper();
                SaveUuid(filePath, folderPath, uuid);
            }
            return uuid;
        }

        private static bool IsValidUuid(string uuid)
        {
            Regex uuidRegex = new Regex(@"^[0-9A-F]{8}-[0-9A-F]{4}-4[0-9A-F]{3}-[89AB][0-9A-F]{3}-[0-9A-F]{12}$");
            return uuidRegex.IsMatch(uuid);
        }
    }
}
