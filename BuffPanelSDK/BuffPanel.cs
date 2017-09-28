using System;
using System.Collections.Generic;
using System.Threading;
using BuffPanel.Logging;
using System.Net;
using System.IO;

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

		private static string serviceHostname = "api.buffpanel.com";
		private static string servicePath = "/run_event/create";

		private static Thread worker = null;
		private static BuffPanel instance = null;

		private string url;
		private string httpBody;
		private Logger logger;

		public static void Track(string gameToken, string playerToken, bool isExistingPlayer, Logger logger = null)
		{
			Logger innerLogger = (logger != null) ? logger : new NullLogger();

			if (instance == null)
			{
				string httpBody = Json.Serialize(new Dictionary<string, object>	{
					{ "game_token", gameToken },
					{ "player_token", playerToken },
					{ "is_existing_player", isExistingPlayer},
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

		public static void Track(string gameToken, string playerToken, bool isExistingPlayer, Dictionary<string, string> attributes, Logger logger = null)
		{
			Logger innerLogger = (logger != null) ? logger : new NullLogger();

			if (instance == null)
			{
				string httpBody = Json.Serialize(new Dictionary<string, object>	{
					{ "game_token", gameToken },
					{ "player_token", playerToken },
					{ "is_existing_player", isExistingPlayer},
					{ "attributes", attributes}
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

	}
}
