using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using System.Timers;
using System.Threading;
using System.Collections.Concurrent;
using BuffPanel.Commands;
using BuffPanel.Logging;


namespace BuffPanel
{

    public class BuffPanel
    {

        private static volatile BuffPanel instance;
        private static object getInstanceLock = new Object();
        private static object initializeFinalizeLock = new Object();
        private static object sessionLock = new Object();

        private bool initialized = false;
        private string appVersion = null;
        private double sessionStartTimestamp = double.NaN;
        private Dictionary<string, object> deviceProperties = null;

        private Logger logger = null;
        private State state = null;
        private volatile BlockingCollection<Command> queue = new BlockingCollection<Command>();
        private Thread worker = null;

        private BuffPanel()
        {
            deviceProperties = Device.GetProperties();
        }

        public static BuffPanel GetInstance()
        {
            if (instance == null)
            {
                lock (getInstanceLock)
                {
                    if (instance == null) instance = new BuffPanel();
                }
            }
            return instance;
        }

        public string Version
        {
            get { return Constants.VERSION; }
        }

        public void Initialize(string projectToken, string appVersion, string target, Logger logger, string workingDirectory)
        {
            lock (initializeFinalizeLock)
            {
                if (initialized) throw new System.InvalidOperationException("BuffPanel client has been already initialized");
                this.appVersion = appVersion;
                initialized = true;
                this.logger = logger;
                state = new State(projectToken, target, logger, workingDirectory);
                CommandManager manager = new CommandManager(queue, state);
                worker = new Thread(new ThreadStart(manager.Consume));
                worker.Start();
            }
        }

        public void Initialize(string projectToken, string appVersion, string target)
        {
            Initialize(projectToken, appVersion, target, null, null);
        }

        public void Initialize(string projectToken, string appVersion)
        {
            Initialize(projectToken, appVersion, null, null, null);
        }

        public void Initialize(string projectToken)
        {
            Initialize(projectToken, null, null, null, null);
        }

        public void Identify(string registeredId)
        {
            Identify(new Dictionary<string, string>() { { Constants.ID_REGISTERED, registeredId } }, null);
        }

        public void Identify(Dictionary<string, string> customerIds)
        {
            Identify(customerIds, null);
        }

        public void Identify(string registeredId, Dictionary<string, object> properties)
        {
            Identify(new Dictionary<string, string>() { { Constants.ID_REGISTERED, registeredId } }, properties);
        }

        public void Identify(Dictionary<string, string> customerIds, Dictionary<string, object> properties)
        {
            Enqueue(new IdentifyCommand(customerIds, properties));
        }

        public void Unidentify()
        {
            Enqueue(new UnidentifyCommand());
        }

        public void Update(Dictionary<string, object> properties)
        {
            Enqueue(new UpdateCommand(properties));
        }

        public void Track(string type)
        {
            Track(type, null, double.NaN);
        }

        public void Track(string type, Dictionary<string, object> properties)
        {
            Track(type, properties, double.NaN);
        }

        public void Track(string type, Dictionary<string, object> properties, double timestamp)
        {
            Enqueue(new TrackCommand(type, properties, timestamp));
        }

        private void Enqueue(Command command)
        {
            try
            {
                queue.Add(command);
            }
            catch (Exception e)
            {
                lock (initializeFinalizeLock)
                {
                    // this should be synchronized since we change lock during initialize call
                    if (logger != null && logger.IsLevelEnabled(Level.Error)) logger.Log(Level.Error, e.Message);
                }
            }
        }

        public void TrackSessionStart()
        {
            TrackSessionStart(null);
        }

        public void TrackSessionStart(Dictionary<string, object> properties)
        {
            lock (sessionLock)
            {
                if (!double.IsNaN(sessionStartTimestamp))
                {
                    TrackSessionEnd(properties);
                }

                Dictionary<string, object> mergedProperties = MergeAutomaticProperties(properties);

                sessionStartTimestamp = Utils.GetCurrentTimestamp();
                Track(Constants.EVENT_SESSION_START, mergedProperties, sessionStartTimestamp);
            }
        }

        public void TrackSessionEnd()
        {
            TrackSessionEnd(null);
        }

        public void TrackSessionEnd(Dictionary<string, object> properties)
        {
            lock (sessionLock)
            {
                double timestamp = Utils.GetCurrentTimestamp();

                Dictionary<string, object> mergedProperties = MergeAutomaticProperties(properties);
                if (!double.IsNaN(sessionStartTimestamp))
                {
                    mergedProperties.Add(Constants.PROPERTY_DURATION, timestamp - sessionStartTimestamp);
                }

                sessionStartTimestamp = double.NaN;
                Track(Constants.EVENT_SESSION_END, mergedProperties, timestamp);
            }
        }

        public void TrackVirtualPayment(string currency, long amount, string itemName, string itemType)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>(deviceProperties);
            properties.Add(Constants.PROPERTY_CURRENCY, currency);
            properties.Add(Constants.PROPERTY_AMOUNT, amount);
            properties.Add(Constants.PROPERTY_ITEM_NAME, itemName);
            properties.Add(Constants.PROPERTY_ITEM_TYPE, itemType);
            this.Track(Constants.EVENT_VIRTUAL_PAYMENT, properties);
        }

        private Dictionary<string, object> MergeAutomaticProperties(Dictionary<string, object> properties)
        {
            lock (initializeFinalizeLock)
            {
                // this should be synchronized since during initialize appVersion can be altered
                Dictionary<string, object> mergedProperties = new Dictionary<string, object>(deviceProperties);
                if (appVersion != null)
                {
                    mergedProperties.Add(Constants.PROPERTY_APP_VERSION, appVersion);
                }
                if (properties != null)
                {
                    Utils.ExtendDictionary(mergedProperties, properties);
                }
                return mergedProperties;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool sendAllQueued)
        {
            lock (initializeFinalizeLock)
            {
                if (!initialized) throw new System.InvalidOperationException("BuffPanel client is not initialized");
                state.sendAllQueued = sendAllQueued;
                BlockingCollection<Command> oldQueue = queue;
                queue = new BlockingCollection<Command>();
                oldQueue.CompleteAdding();
                worker.Join();
                initialized = false;
            }
        }

    }
}
