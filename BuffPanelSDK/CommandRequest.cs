using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuffPanel
{
    internal class CommandRequest
    {

        public int id;
        public Dictionary<string, object> data;
        public int retries;

        public CommandRequest(int id, Dictionary<string, object> data, int retries)
        {
            this.id = id;
            this.data = data;
            this.retries = retries;
        }

    }
}
