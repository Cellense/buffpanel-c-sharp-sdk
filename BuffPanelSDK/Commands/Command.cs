using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuffPanel;

namespace BuffPanel.Commands
{

    internal interface Command
    {
        void Execute(State state);
    }

}
