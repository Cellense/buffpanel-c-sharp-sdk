using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BuffPanel.Logging;
using System.Net;
using System.IO;

namespace BuffPanel
{
    internal class Sender
    {

        private State state;
        private Db db;
        private CancellationTokenSource cancellationTokenSource;
        private Logger logger;

        private string endpoint = null;

        public volatile bool hasData = false, terminateBulk = false;

        public Sender(State state, Db db, CancellationTokenSource cancellationTokenSource, Logger logger)
        {
            this.state = state;
            this.db = db;
            this.cancellationTokenSource = cancellationTokenSource;
            this.logger = logger;
            endpoint = state.target + Constants.BULK_URL;
        }

        public void Consume()
        {
            if (!terminateBulk) SendAllBulks();
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                cancellationTokenSource.Token.WaitHandle.WaitOne(Constants.BULK_INTERVAL_MS);
                if (hasData && !terminateBulk)
                {
                    hasData = false;
                    SendAllBulks();
                }
            }
        }

        private void SendAllBulks()
        {
            int retries = 0, lastWait = Constants.BULK_INTERVAL_MS;
            List<CommandRequest> bulk = null;
            do
            {
                bulk = db.ReadFirst();
                if (bulk.Count == 0 || terminateBulk) return;
                if (!SendBulk(bulk))
                {

                    // retry should take place
                    // Please note that this is retry of whole request in case that client or API is offline.
                    // This retry won't be executed (and counter of task retries won't be incremented) in case that retry was caused
                    // by our API telling us to retry the command. Our API retries commands only when we sent too many at once, 
                    // in that case counter should not be incremented.
                    List<int> ids = new List<int>();
                    foreach (var command in bulk) ids.Add(command.id);

                    db.IncrementRetries(ids);
                    if (lastWait <= Constants.BULK_MAX_RETRY_WAIT_MS)
                    {
                        lastWait *= 2;
                    }
                    else
                    {
                        lastWait = Constants.BULK_MAX_RETRY_WAIT_MS;
                    }
                    retries++;

                    if (cancellationTokenSource.Token.WaitHandle.WaitOne(lastWait))
                    {
                        return;
                    }
                }

            } while (bulk.Count > 0 && !terminateBulk);
        }

        private bool SendBulk(List<CommandRequest> commands)
        {

            double timestamp = Utils.GetCurrentTimestamp();
            List<object> convertedCommands = ConvertCommands(commands, timestamp);

            Dictionary<string, object> payload = new Dictionary<string, object>() { { "commands", convertedCommands } };

            try
            {
                List<int> commandsToRemove = new List<int>();
                int successCount = 0;

                WebRequest request = SendBulkRequest(payload);

                var httpResponse = (HttpWebResponse) request.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    if (result != null)
                    {
                        var resultParse = Json.Deserialize(result) as Dictionary<string, object>;
                        if (resultParse != null)
                        {
                            for (int i = 0; i < commands.Count; i++)
                            {
                                var statusParse = ((List<object>) resultParse["results"])[i] as Dictionary<string, object>;
                                string status = ((string) statusParse["status"]).ToLower();
                                if (status.Equals("ok"))
                                {

                                    // ok
                                    commandsToRemove.Add(commands[i].id);
                                    successCount++;

                                }
                                else if (status.Equals("error"))
                                {

                                    // error
                                    if (logger.IsLevelEnabled(Level.Debug))
                                    {
                                        logger.Log(Level.Debug, "Failed command: " + Json.Serialize(commands[i].data));
                                    }

                                    commandsToRemove.Add(commands[i].id);
                                }

                                // retry otherwise
                            }
                        }
                    }
                }

                db.RemoveCommands(commandsToRemove);

                if (logger.IsLevelEnabled(Level.Debug))
                {
                    StringBuilder sb = new StringBuilder("Batch executed, ")
                        .Append(commands.Count).Append(" enqueued, ")
                        .Append(successCount).Append(" processed, ")
                        .Append(commandsToRemove.Count - successCount).Append(" failed, rest was told to retry");
                    logger.Log(Level.Debug, sb.ToString());
                }

            }
            catch (WebException e)
            {
                if (logger.IsLevelEnabled(Level.Error)) logger.Log(Level.Error, e.Message);
                return false;
            }

            return true;
        }

        private List<object> ConvertCommands(List<CommandRequest> commands, double nowTimestamp)
        {
            List<object> result = new List<object>();
            foreach (CommandRequest r in commands)
            {
                var data = (Dictionary<string, object>) r.data["data"];
                if (data.ContainsKey("local_timestamp"))
                {
                    data["age"] = nowTimestamp - Convert.ToDouble(data["local_timestamp"]);
                }
                result.Add(r.data);
            };
            return result;
        }

        private WebRequest SendBulkRequest(object payload)
        {
            WebRequest request = WebRequest.Create(endpoint);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Timeout = Constants.BULK_TIMEOUT_MS;

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                string json = Json.Serialize(payload);
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }

            return request;
        }

        public void Notify()
        {
            hasData = true;
        }

    }
}
