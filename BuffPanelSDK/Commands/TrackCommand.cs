using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuffPanel.Commands
{
    internal class TrackCommand : Command
    {

        private string eventType;
        private Dictionary<string, object> properties;
        private double timestamp;

        public TrackCommand(string eventType, Dictionary<string, object> properties, double timestamp)
        {
            this.eventType = eventType;
            this.properties = properties;
            this.timestamp = Utils.IsDoubleDefined(timestamp) ? timestamp : Utils.GetCurrentTimestamp();
        }

        public void Execute(State state)
        {
            Dictionary<string, object> data = new Dictionary<string, object>() {
                {"name", Constants.ENDPOINT_TRACK},
                {"data", new Dictionary<string, object>() {
                    {"customer_ids", state.customerIds},
                    {"project_id", state.projectToken},
                    {"type", eventType},
                    {"properties", (properties != null) ? properties : new Dictionary<string, object>()},
                    {"local_timestamp", timestamp} 
                }}
            };
            state.QueueCommand(Json.Serialize(data));
        }
    }
}
