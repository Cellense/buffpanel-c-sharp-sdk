using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuffPanel.Logging;
using System.Threading;

namespace BuffPanel
{

    internal class State
    {

        public string projectToken;
        public string target;
        public Logger logger;
        public volatile bool sendAllQueued;
        public Dictionary<string, string> customerIds;

        private Db db;
        private Sender sender;
        private Thread worker = null;
        private CancellationTokenSource cancellationTokenSource;

        public State(string projectToken, string target, Logger logger, string workingDirectory)
        {
            this.projectToken = projectToken;
            this.target = (target != null) ? target : Constants.DEFAULT_TARGET;
            this.logger = (logger != null) ? logger : new NullLogger();
            db = new Db(this.logger, workingDirectory);
        }

        public void Initialize()
        {
            db.Initialize();
            customerIds = db.GetIdentifiers();

            if (!customerIds.ContainsKey(Constants.ID_COOKIE))
            {
                string cookieId = Utils.GenerateCookieId();
                customerIds.Add(Constants.ID_COOKIE, cookieId);
                db.SetIdentifier(Constants.ID_COOKIE, cookieId);
            }

            cancellationTokenSource = new CancellationTokenSource();
            sender = new Sender(this, db, cancellationTokenSource, this.logger);
            worker = new Thread(new ThreadStart(sender.Consume));
            worker.Start();
        }

        public void StoreIds()
        {
            db.SetIdentifiers(customerIds);
        }

        public void RemoveIds()
        {
            customerIds.Clear();
            db.RemoveIdentifiers();
            string cookieId = Utils.GenerateCookieId();
            customerIds.Add(Constants.ID_COOKIE, cookieId);
            db.SetIdentifier(Constants.ID_COOKIE, cookieId);
        }

        public void QueueCommand(string serializedCommand)
        {
            db.AddCommand(serializedCommand);
            sender.Notify();
        }

        public void SendAllNow()
        {
            cancellationTokenSource.Cancel();
            worker.Join();
        }

        public void StopSending()
        {
            cancellationTokenSource.Cancel();
            sender.terminateBulk = true;
            worker.Join();
        }
    }
}
