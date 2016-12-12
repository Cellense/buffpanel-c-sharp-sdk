using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuffPanel.Commands
{
    internal class IdentifyCommand : Command
    {

        private Dictionary<string, string> customerIds;
        private Dictionary<string, object> properties;
        private double timestamp;

        public IdentifyCommand(Dictionary<string, string> customerIds, Dictionary<string, object> properties)
        {
            this.customerIds = customerIds;
            this.properties = properties;
            timestamp = Utils.GetCurrentTimestamp();
        }

        public void Execute(State state)
        {
            Utils.ExtendDictionary(state.customerIds, customerIds);
            state.StoreIds();

            // Track identification event
            Dictionary<string, object> properties = Device.GetProperties();

            // I would prefer to add all the ids but I can't do that without breaking backwards 
            // compatibility since `registered` identifier was called `registration_id`
            if (state.customerIds.ContainsKey(Constants.ID_REGISTERED))
            {
                properties.Add(Constants.PROPERTY_REGISTRATION_ID, state.customerIds[Constants.ID_REGISTERED]);
            }
            new TrackCommand(Constants.EVENT_IDENTIFICATION, properties, timestamp).Execute(state);
            new UpdateCommand(properties).Execute(state);
        }
    }
}
