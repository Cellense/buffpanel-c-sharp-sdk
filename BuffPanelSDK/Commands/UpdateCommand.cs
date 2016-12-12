using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuffPanel.Commands
{
    internal class UpdateCommand : Command
    {

        private Dictionary<string, object> properties;

        public UpdateCommand(Dictionary<string, object> properties)
        {
            this.properties = properties;
        }

        public void Execute(State state)
        {
            Dictionary<string, object> data = new Dictionary<string, object>() {
                {"name", Constants.ENDPOINT_UPDATE},
                {"data", new Dictionary<string, object>() {
                    {"ids", state.customerIds},
                    {"project_id", state.projectToken},                    
                    {"properties", (properties != null) ? properties : new Dictionary<string, object>()}                    
                }}
            };
            state.QueueCommand(Json.Serialize(data));
        }
    }
}
