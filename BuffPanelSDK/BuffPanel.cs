using System;
using System.Collections.Generic;
using System.Threading;
using BuffPanel.Logging;
using System.Net;
using System.IO;
using System.Reflection;

namespace BuffPanel
{
	public class BuffPanel
	{
		internal class BuffPanelException: Exception
		{
			public BuffPanelException(string message): base(message)
			{
			}
		}

		private static int requestTimeout = 10000;
		private static int baseRetryTimeout = 200;
		private static int maxRetries = 10;

		public static string serviceHostname = "buffpanel.com:8080";
        public static string servicePath = "/api/run";
        public static string redirectURI = ".trbt.it";

		private static Thread worker = null;
		private static BuffPanel instance = null;

		private string url;
		private string httpBody;
		private Logger logger;

        public static Assembly sqllite;
        public static Assembly protecteddata;

        private static void WriteResourceToFile(string resourceName, string fileName)
        {
            using (var resource = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                using (var file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    resource.CopyTo(file);
                }
            }
        }

        public static void Track(string gameToken, string playerToken, Logger logger = null)
		{
			Track(gameToken, new Dictionary<string, object> { { "registered", playerToken } }, logger);
		}

		public static void Track(string gameToken, Dictionary<string, object> playerTokens, Logger logger = null)
		{
            Logger innerLogger = (logger != null) ? logger : new NullLogger();
            Console.WriteLine(Directory.GetCurrentDirectory());
            if (!Directory.Exists(@"BuffPanel\"))
            {
                Directory.CreateDirectory(@"BuffPanel\");
                if (!File.Exists(@"BuffPanel\System.Data.SQLite.dll")) WriteResourceToFile("BuffPanel.BuffPanelSDK.System.Data.SQLite.dll", @"BuffPanel\System.Data.SQLite.dll");
                sqllite = Assembly.Load(File.ReadAllBytes(@"BuffPanel\System.Data.SQLite.dll"));
                innerLogger.Log(Level.Debug, BuffPanel.sqllite.FullName.ToString());
                if (!File.Exists(@"BuffPanel\System.Security.Cryptography.ProtectedData.dll")) WriteResourceToFile("BuffPanel.BuffPanelSDK.System.Security.Cryptography.ProtectedData.dll", @"BuffPanel\System.Security.Cryptography.ProtectedData.dll");
                protecteddata = Assembly.Load(File.ReadAllBytes(@"BuffPanel\System.Security.Cryptography.ProtectedData.dll"));
                Directory.CreateDirectory(@"BuffPanel\x86\");
                if (!File.Exists(@"BuffPanel\x86\SQLite.Interop.dll")) WriteResourceToFile("BuffPanel.BuffPanelSDK.x86.SQLite.Interop.dll", @"BuffPanel\x86\SQLite.Interop.dll");
                Directory.CreateDirectory(@"BuffPanel\x64\");
                if (!File.Exists(@"BuffPanel\x64\SQLite.Interop.dll")) WriteResourceToFile("BuffPanel.BuffPanelSDK.x64.SQLite.Interop.dll", @"BuffPanel\x64\SQLite.Interop.dll");
            }

			if (instance == null)
			{
				string httpBody = CreateHttpBody(gameToken, playerTokens, innerLogger);
                Console.WriteLine(httpBody);
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

        private static string CreateHttpBody(string gameToken, Dictionary<string, object> playerTokens, Logger logger = null)
		{
			Logger innerLogger = (logger != null) ? logger : new NullLogger();
			Dictionary<string, object> playerTokensDict = new Dictionary<string, object>();
            if (playerTokens.ContainsKey("registered"))
			{
				playerTokensDict.Add("registered", playerTokens["registered"]);
			}
			if (playerTokens.ContainsKey("user_id"))
			{
				playerTokensDict.Add("user_id", playerTokens["user_id"]);
			}
			if (playerTokensDict.Count == 0)
			{
				return null;
			}

            Dictionary<string, string> cookies = new Dictionary<string, string>();
            try
            {
                cookies = CookieExtractor.ReadCookies(gameToken, innerLogger);
            } catch (Exception e)
            {
                innerLogger.Log(Level.Error, e.Message + " " + e.StackTrace);
            }
            return Json.Serialize(new Dictionary<string, object>
			{
				{ "game_token", gameToken },
				{ "player_tokens", playerTokensDict },
				{ "browser_cookies", cookies }
			});
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

	}
}
